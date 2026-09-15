# TestVault - Local Development Setup

Companion to [`DEPLOYMENT.md`](DEPLOYMENT.md), which covers the production
(IIS) side. This covers running `TestVault.Web` on a developer machine.

**Scope**: `TestVault.Web` (the ASP.NET Core API) only.

---

## 1. Why `dotnet run` fails with no setup

`TestVault.Web` refuses to start without two pieces of configuration:

- `Jwt:Secret` - 32+ characters (`Program.cs` fails fast at startup if
  missing/too short, on purpose - see that check's own comment).
- `ConnectionStrings:DefaultConnection` - the SQL Server connection string
  (`SqlConnectionFactory` throws if missing).

Neither has a real value in `appsettings.json` / `appsettings.Development.json`
by design (see those files' own `"// NOTE"` entries) - a real secret must
never be committed to Git. Locally, the correct place for a real value is
**.NET User Secrets**, not `appsettings.*.json` and not a manually-typed
`$env:` variable every session.

## 2. One-time setup: User Secrets

User Secrets store real values in a JSON file **outside the repo**
(`%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` on Windows), keyed to
`TestVault.Web.csproj`'s `<UserSecretsId>`. `WebApplication.CreateBuilder`
automatically loads this file whenever `ASPNETCORE_ENVIRONMENT=Development`
(the default for `dotnet run`, via `launchSettings.json`) - no code change
needed.

From the `TestVault.Web` folder, run once:

```powershell
dotnet user-secrets set "Jwt:Secret" "<any random string, 32+ chars>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<your-sql-server>;Database=TestVault;User Id=sa;Password=<your-password>;TrustServerCertificate=True"

# Optional - only needed to exercise AI failure analysis locally.
dotnet user-secrets set "AI:ApiKey" "<your Anthropic API key>"
```

Generate a `Jwt:Secret` with:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))
```

Verify what's stored (values are printed in plain text - this file is not
encrypted, it's just outside source control):

```powershell
dotnet user-secrets list
```

After this, both `dotnet run` and `dotnet TestVault.Web.dll` (run with
`ASPNETCORE_ENVIRONMENT=Development` set - see §4) pick these up
automatically, every time, with nothing to retype.

**Never** paste a real secret into `appsettings.json` or
`appsettings.Development.json` "just for a moment" - those files are
tracked in Git, and an uncommitted edit is one accidental `git add .` away
from being pushed.

## 3. Running with `dotnet run`

```powershell
cd TestVault.Web
dotnet run
```

This uses the `http` (or `https`) profile in
[`Properties/launchSettings.json`](../TestVault.Web/Properties/launchSettings.json),
which already binds to `http://localhost:5238` (`https://localhost:7164`)
and sets `ASPNETCORE_ENVIRONMENT=Development` - not port 5000, so it won't
collide with another Kestrel app's default binding. Swagger opens
automatically at `/swagger`.

To use a different port without editing the tracked `launchSettings.json`,
add your own profile to a `launchSettings.json`-adjacent, git-ignored
override (`dotnet run --launch-profile <name>` still reads
`launchSettings.json` itself, so simplest is just editing the port there if
your machine genuinely needs a different one - it's local dev config, not a
secret, and collisions are per-developer).

## 4. Running the published output directly (`dotnet TestVault.Web.dll`)

This is what you were doing during "local publish testing". Two things
behave differently here than under `dotnet run`:

1. **`launchSettings.json` is not read at all** - it's a `dotnet run`/IDE
   convenience only. Without it, ASP.NET Core defaults
   `ASPNETCORE_ENVIRONMENT` to `Production`, which loads
   `appsettings.Production.json` - and that file's secrets are
   intentionally blank (see `DEPLOYMENT.md` §4). This, not a missing
   environment variable, is why you saw `Jwt:Secret is not configured` and
   the connection-string error even though `appsettings.Development.json`
   has real values via User Secrets now.
2. **No port is pre-selected** - Kestrel falls back to its own default
   (`http://localhost:5000`), which is what collided with whatever else on
   your machine was already using 5000.

Fix both by being explicit when you run the published output:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"   # loads appsettings.Development.json + User Secrets
dotnet TestVault.Web.dll --urls "http://localhost:5238"
```

Setting `ASPNETCORE_ENVIRONMENT` for your whole user account (System
Properties -> Environment Variables, once) removes the need to type it per
session; `--urls` (or `$env:ASPNETCORE_URLS`) avoids the port-5000 clash
without hardcoding a port anywhere in source. **Do not** set
`ASPNETCORE_ENVIRONMENT=Development` on the actual IIS server - production
must run as `Production` so it never loads dev-only configuration or User
Secrets.

## 5. What NOT to do

- Don't put real secrets in `appsettings.json` / `appsettings.Development.json`
  / `appsettings.Production.json` - they're tracked in Git.
- Don't commit a personal `launchSettings.json` change that embeds a real
  secret in `environmentVariables` - it's tracked too.
- Don't reuse your local dev JWT secret or DB password in any shared
  environment (UAT/production) - each environment gets its own value
  (`DEPLOYMENT.md` §4.1 already calls this out for production).
