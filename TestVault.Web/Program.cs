using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TestVault.Application;
using TestVault.Application.Interfaces;
using TestVault.Application.Security;
using TestVault.Infrastructure;
using TestVault.Infrastructure.Identity;
using TestVault.Web.BackgroundJobs;
using TestVault.Web.Hubs;
using TestVault.Web.Json;
using TestVault.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Fail fast, with a clear message, rather than let a missing/too-short JWT
// secret surface later as a confusing crypto exception on the first login
// attempt (or worse, an app that "works" but signs tokens with a weak key).
// Mirrors JwtTokenService's own runtime check - this one exists so the
// problem is caught at startup, before the app accepts any requests at all.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret is not configured (or is shorter than 32 characters). " +
        "Set it via an environment variable (Jwt__Secret) or User Secrets - never commit it to appsettings.json. " +
        "Generate one with e.g. `openssl rand -base64 32`.");
}

// ==========================================================================
// Services (composition root)
// ==========================================================================

const string FrontendCorsPolicy = "FrontendCors";

// Controllers - TestVault.Web exposes a controller-based Web API (matches
// the existing Node/Express route structure this project will replace:
// /api/runs, /api/tests, /api/manual, /api/analysis).
//
// JSON options force snake_case property names (total_runs, root_cause,
// started_at, ...) and camelCase-cased enum strings ("failed", "timedOut")
// so the response bytes match what the existing React frontend already
// consumes - see SnakeCaseJsonNamingPolicy and each enum's own doc comments
// for why camelCase (not snake_case) is correct for enum values.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = SnakeCaseJsonNamingPolicy.Instance;
    options.JsonSerializerOptions.DictionaryKeyPolicy = SnakeCaseJsonNamingPolicy.Instance;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

// CORS - the existing Node app allowed cross-origin requests from
// FRONTEND_URL (backend/server.js:20); without this, the React frontend
// cannot call this API (or the SignalR hub below) from the browser at all.
// AllowCredentials is required for SignalR's negotiate handshake.
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SignalR - replaces the Socket.IO server in backend/server.js. See
// Hubs/RunHub.cs and Hubs/IRunHubClient.cs.
builder.Services.AddSignalR();

// IRunNotifier's SignalR implementation, and the background service that
// actually executes queued manual runs (see BackgroundJobs/ManualRunBackgroundService.cs).
// Registered here, not in TestVault.Infrastructure/DependencyInjection.cs,
// because both depend on Web-only types (RunHub/IHubContext, BackgroundService).
builder.Services.AddSingleton<IRunNotifier, SignalRRunNotifier>();
builder.Services.AddHostedService<ManualRunBackgroundService>();

// OpenAPI/Swagger - development-time API exploration only (disabled in the
// request pipeline below when not Development).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TestVault API",
        Version = "v1",
        Description = "Playwright test-run dashboard API: run history and stats, individual test-case results, and AI-assisted failure analysis. Replaces the original Node/Express API (backend/routes/*.js) one-for-one, plus a few endpoints (noted per-action below) added for this phase's controller shape."
    });

    // Pulls controller/action <summary>/<param>/<response> XML doc comments
    // into the generated UI - see the GenerateDocumentationFile/NoWarn CS1591
    // settings in TestVault.Web.csproj.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Lets Swagger UI's "Authorize" button attach a bearer token to every
    // subsequent try-it-out call - paste just the raw JWT (no "Bearer "
    // prefix; the scheme adds that automatically).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by POST /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Authentication - validates the JWTs AuthController issues. No cookie
// scheme (see AddIdentityCore's own comment in DependencyInjection.cs) -
// JwtBearer is the only way to authenticate a request to this API.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TestVault",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TestVault",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            // Tighter than the 5-minute default: access tokens are already
            // short-lived (Jwt:AccessTokenMinutes), so a large allowance
            // here would meaningfully extend how long an expired token
            // keeps working.
            ClockSkew = TimeSpan.FromSeconds(30),

            // Must match the claim types JwtTokenService actually writes
            // ("role" / JwtRegisteredClaimNames.UniqueName) rather than
            // relying on JwtSecurityTokenHandler's inbound claim-type
            // remapping, whose defaults have changed across .NET versions -
            // explicit here means [Authorize(Roles = ...)] and User.Identity.Name
            // behave the same regardless of that default.
            RoleClaimType = "role",
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName
        };

        // SignalR JS clients can't set a custom Authorization header on the
        // underlying WebSocket/SSE transports a browser opens - the
        // standard workaround is to send the token as an access_token query
        // parameter instead (signalRService.js's accessTokenFactory), which
        // this reads for hub requests specifically. Everything else keeps
        // authenticating the normal way (Authorization: Bearer header).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Authorization - a fallback policy requires authentication on every
// endpoint by default; [AllowAnonymous] (AuthController) is the only way
// out, and [Authorize(Roles = ...)] layers stricter role requirements on
// top for Manual/Analysis/Admin, per this phase's requirements.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Application - RunsService, TestsService, AiAnalysisService, ManualRunService
// (TestVault.Application/ServiceCollectionExtensions.cs).
builder.Services.AddApplication();

// Infrastructure - SQL connection factory + Dapper repositories, the AI
// provider's typed HttpClient, and ASP.NET Core Identity (EF Core, its own
// DbContext) (TestVault.Infrastructure/DependencyInjection.cs). Reads
// "DefaultConnection", "AI:ApiKey" from configuration; see appsettings.json.
builder.Services.AddInfrastructure(builder.Configuration);

// Liveness endpoint for the deployment pipeline (Phase 8's health check
// step) and any future load balancer/monitoring - a shallow check (the
// process is up and the ASP.NET Core pipeline is responding), not a deep
// dependency check. No prior phase added one; discovered missing while
// building the deploy workflow, whose only alternative health signal
// (GET /) has nothing mapped to it and would 404.
builder.Services.AddHealthChecks();

var app = builder.Build();

// ==========================================================================
// DEV-ONLY: "forgot the admin password" console command
// ==========================================================================
//
// There is no self-service "forgot password" flow in this app (no email
// sending - see Phase 6's noted gaps), and the bootstrap admin's initial
// password is a one-time random value shown only once, in this app's own
// startup log (IdentitySeeder). Lose it and there is otherwise no way back
// in short of hand-editing the database - which is exactly the unsafe
// shortcut this command exists to avoid.
//
// This is a CONSOLE COMMAND, not an HTTP endpoint: it runs to completion
// and the process exits before Kestrel ever starts listening, so there is
// no route to remember to remove before deploying, and nothing reachable
// over the network even by accident. It goes through the same UserManager
// APIs a real self-service reset would use - GeneratePasswordResetTokenAsync
// then ResetPasswordAsync - so the new password is still validated against
// Identity's configured password policy (DependencyInjection.cs) and
// PasswordHash is never touched directly.
//
// Gated on IsDevelopment() as defense in depth, independent of how it's
// invoked - even run by hand against a Production-configured deployment
// (e.g. someone RDPs into the IIS box and runs the dll directly), it
// refuses to do anything.
//
//   dotnet run -- reset-password <username> <newPassword>
//
if (args.Length > 0 && string.Equals(args[0], "reset-password", StringComparison.OrdinalIgnoreCase))
{
    return await RunResetPasswordCommandAsync(app, args);
}

// One-time startup seeding: the three roles, and a bootstrap admin account
// if the database has no users at all yet - see IdentitySeeder's own doc
// comment for why this doesn't use a fixed default password.
//
// Deliberately non-fatal: on a fresh environment the Identity schema/DB
// might not be reachable or migrated yet (see Database/identity-schema.sql).
// Crashing the whole app over that would take down every other endpoint too
// (including ones with no DB dependency at all) - log loudly instead and
// let startup continue; auth-dependent endpoints will then fail per-request
// with a clear error via ExceptionHandlingMiddleware, exactly like any other
// DB-dependent endpoint already does.
using (var scope = app.Services.CreateScope())
{
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await IdentitySeeder.SeedAsync(scope.ServiceProvider, seedLogger);
    }
    catch (Exception ex)
    {
        seedLogger.LogError(ex, "Identity seeding failed - is the database reachable and migrated (Database/identity-schema.sql)? Continuing startup regardless.");
    }
}

// ==========================================================================
// HTTP request pipeline
// ==========================================================================

// Registered first so it wraps every other middleware/endpoint below and
// can translate any exception they throw - see Middleware/ExceptionHandlingMiddleware.cs.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Applies to every response, including error responses the middleware
// above just wrote - see Middleware/SecurityHeadersMiddleware.cs.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Serves Playwright's failure screenshots/videos/traces, replacing
// backend/server.js's app.use('/screenshots', express.static('screenshots'))
// (and the equivalent for videos/traces) - discovered missing while wiring
// up the frontend's screenshot/trace links in this phase. Public/unauthenticated,
// matching the original exactly (static middleware fully handles a matching
// request and never reaches UseAuthorization below, regardless of pipeline
// order - this isn't a new gap, just preserved prior behavior).
//
// Rooted at Playwright:WorkingDirectory (the same setting
// PlaywrightTestRunnerProcess uses, see Phase 5) rather than this app's own
// content root - Playwright writes these files relative to wherever
// `npx playwright test` actually runs, which in a real deployment is not
// necessarily where TestVault.Web is published.
var artifactsRoot = app.Configuration["Playwright:WorkingDirectory"];
if (string.IsNullOrWhiteSpace(artifactsRoot))
{
    // NOT Directory.GetCurrentDirectory(): under IIS in-process hosting the
    // OS-level current directory is inherited from the IIS worker process
    // itself (historically C:\Windows\System32\inetsrv), not this site's
    // physical path - and the app pool identity has no write access there.
    // ContentRootPath is always this app's own deployment folder, regardless
    // of hosting model (Kestrel/dotnet run, IIS in-process, IIS out-of-
    // process), so it's the only fallback that's actually safe here.
    artifactsRoot = app.Environment.ContentRootPath;
}

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

foreach (var folder in new[] { "screenshots", "videos", "traces" })
{
    var physicalPath = Path.Combine(artifactsRoot, folder);

    // Non-fatal, matching the Identity-seeding block below: a missing/
    // inaccessible artifacts directory (wrong Playwright:WorkingDirectory,
    // restrictive NTFS permissions, disk issues, ...) should degrade to
    // "screenshot/video/trace links don't work" for that one folder, not
    // take down every other endpoint in the app - including /health,
    // which has no dependency on this feature at all.
    try
    {
        Directory.CreateDirectory(physicalPath); // PhysicalFileProvider throws if the root doesn't exist yet

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(physicalPath),
            RequestPath = $"/{folder}"
        });
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to prepare the '{Folder}' artifacts folder at '{Path}' - serving it is disabled, but startup will continue. Set Playwright:WorkingDirectory (env var Playwright__WorkingDirectory) to a path the app pool identity can write to.", folder, physicalPath);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TestVault API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

// Order matters: authentication (who are you?) must run before
// authorization (are you allowed?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// TestVault.Web is API-only (no HomeController/Views/wwwroot - the React
// frontend is a separate app), so "/" never matched any endpoint. With no
// endpoint to attach [AllowAnonymous] to, the global FallbackPolicy above
// still applied to the unmatched request and returned 401 instead of
// letting it fall through to a plain 404. Mapping a trivial anonymous root
// endpoint gives browsers/load balancers hitting "/" a real 200 without
// weakening the fallback policy for every other (still-unmapped) route.
app.MapGet("/", () => Results.Ok(new { service = "TestVault API", status = "running" }))
    .AllowAnonymous();

// Always anonymous, unlike everything else under the global fallback
// policy - a deployment health check or load balancer has no bearer token
// to present, and conventionally never should need one for a liveness probe.
app.MapHealthChecks("/health").AllowAnonymous();

// Phase 5/6 left this hub anonymous, noting it should require auth once the
// frontend actually sends a token - Phase 7's signalRService.js does
// exactly that (accessTokenFactory -> ?access_token= query param, read by
// the OnMessageReceived hook above), so the hub now falls under the same
// global authentication requirement as every other endpoint.
app.MapHub<RunHub>("/hubs/run");

app.Run();
return 0;

// See the "DEV-ONLY" block above for why this exists and why it's a
// console command rather than a controller action.
static async Task<int> RunResetPasswordCommandAsync(WebApplication app, string[] args)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("reset-password is a development-only tool and is disabled outside the Development environment.");
        return 1;
    }

    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: dotnet run -- reset-password <username> <newPassword>");
        return 1;
    }

    var username = args[1];
    var newPassword = args[2];

    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var user = await userManager.FindByNameAsync(username);
    if (user is null)
    {
        Console.Error.WriteLine($"No user named '{username}' was found.");
        return 1;
    }

    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
    var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);

    if (!result.Succeeded)
    {
        Console.Error.WriteLine("Password reset failed:");
        foreach (var error in result.Errors)
        {
            Console.Error.WriteLine($" - {error.Description}");
        }
        return 1;
    }

    Console.WriteLine($"Password for '{username}' has been reset successfully.");
    return 0;
}
