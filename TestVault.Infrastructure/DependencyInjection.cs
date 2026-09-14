using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestVault.Application.Interfaces;
using TestVault.Infrastructure.Data;
using TestVault.Infrastructure.ExternalServices;
using TestVault.Infrastructure.Identity;
using TestVault.Infrastructure.InMemory;
using TestVault.Infrastructure.Repositories;

namespace TestVault.Infrastructure;

/// <summary>
/// Composition-root wiring for TestVault.Infrastructure. Called once from
/// TestVault.Web's Program.cs:
///
///     builder.Services.AddInfrastructure(builder.Configuration);
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Singleton: reads the connection string once from configuration
        // and hands out a new SqlConnection per call - safe to share.
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        // Scoped: one instance per request, consistent with the rest of the
        // ASP.NET Core request-scoped service conventions.
        services.AddScoped<IRunsRepository, RunsRepository>();
        services.AddScoped<ITestCasesRepository, TestCasesRepository>();
        services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();

        // Typed HttpClient for the AI provider - see
        // ExternalServices/ClaudeAiAnalysisProvider. Swapping providers
        // means registering a different implementation here; nothing in
        // TestVault.Application changes.
        services.AddHttpClient<IAiAnalysisProvider, ClaudeAiAnalysisProvider>();

        // Singleton: shared "is a run active" state that must survive
        // across the separate HTTP requests that start/stop/poll a manual
        // run - see IManualRunTracker/IManualRunQueue's own doc comments.
        services.AddSingleton<IManualRunTracker, ManualRunTracker>();
        services.AddSingleton<IManualRunQueue, ManualRunQueue>();

        // Transient: stateless process wrapper, safe to hand out a fresh
        // instance per call.
        services.AddTransient<ITestRunnerProcess, PlaywrightTestRunnerProcess>();

        // Identity - a separate EF Core DbContext scoped to exactly
        // AspNetUsers/AspNetRoles/.../RefreshTokens (see
        // TestVaultIdentityDbContext's own doc comment for why this never
        // touches test_runs/test_cases/ai_analysis). Same connection string
        // as the Dapper side.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TestVaultIdentityDbContext>(options => options.UseSqlServer(connectionString));

        // AddIdentityCore (not AddIdentity): this is a pure JSON API with no
        // server-rendered login pages, so there's no reason to also register
        // Identity's default cookie authentication scheme - only
        // AddJwtBearer (Program.cs) should ever authenticate a request here.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Hardened beyond ASP.NET Core's own defaults, per this
                // phase's "enterprise-grade" security goal.
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<TestVaultIdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }
}
