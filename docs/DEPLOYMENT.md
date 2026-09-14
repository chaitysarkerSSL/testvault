# TestVault - Production Deployment Guide

Phase 8 of the Node.js → ASP.NET Core migration. This document is the
human-facing companion to
[`.github/workflows/testvault-iis-deploy.yml`](../.github/workflows/testvault-iis-deploy.yml) —
read this before the first deployment, and keep it updated if the workflow
changes.

**Scope**: this covers deploying `TestVault.Web` (the ASP.NET Core API) to
IIS. The React frontend (`frontend/`) is a separate static site with its own
deployment story, out of scope here.

---

## 1. IIS Setup Guide

One-time provisioning on the target Windows Server, before the workflow is
ever run against it. Everything here is verified automatically by the
workflow's **Environment Validation** step, which fails with a specific,
actionable error if something below wasn't done — it does not guess or
silently skip a missing prerequisite.

### 1.1 Install the .NET 8.0 Hosting Bundle

**Not** the SDK the CI runner builds with — a separate installer that adds
the ASP.NET Core Module v2 (ANCM) to IIS, the thing that actually lets IIS
host an ASP.NET Core app at all.

1. Download the **Hosting Bundle** (not "Runtime", not "SDK") for .NET 8.0
   from https://dotnet.microsoft.com/download/dotnet/8.0.
2. Run the installer.
3. Restart IIS so it picks up the module: `net stop was /y` then `net start w3svc`
   (stopping the Windows Activation Service also stops any dependent
   services, including W3SVC; starting W3SVC brings both back).
4. Confirm: `Get-WebGlobalModule -Name AspNetCoreModuleV2` should return a
   result, not an error.

### 1.2 Create the Application Pool

Name: **`TestVault_UAT`** (matches `IIS_APP_POOL` in the workflow — if you
use a different name, update the workflow's `env:` block to match).

Via IIS Manager:

1. Application Pools → Add Application Pool.
2. Name: `TestVault_UAT`.
3. **.NET CLR Version: `No Managed Code`** — this is not optional or
   cosmetic. ANCM hosts the app in-process inside the IIS worker process
   using its own runtime; a pool still configured for classic ASP.NET
   (`v4.0`) will not run it. The workflow checks this exact setting and
   fails deployment with an explicit fix-it message if it's wrong.
4. **Managed Pipeline Mode: `Integrated`**.
5. Leave "Start application pool immediately" checked.

Via PowerShell (equivalent, if you'd rather script it once):

```powershell
Import-Module WebAdministration
New-WebAppPool -Name "TestVault_UAT"
Set-ItemProperty "IIS:\AppPools\TestVault_UAT" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\TestVault_UAT" -Name managedPipelineMode -Value "Integrated"
```

### 1.3 Create the folder structure

```powershell
New-Item -ItemType Directory -Force -Path `
  "E:\AllWebApplication\TestVault", `
  "E:\AllWebApplication\TestVault\Releases", `
  "E:\AllWebApplication\TestVault\Current", `
  "E:\AllWebApplication\TestVault\Backup"
```

(The workflow also does this itself on every run via `New-Item -Force`, so
this step is really just so `Current` exists as an empty, bindable folder
before the very first deployment.)

### 1.4 Create the IIS website

1. Sites → Add Website.
2. Site name: **`TestVault`** (matches `IIS_SITE_NAME` in the workflow).
3. Application pool: `TestVault_UAT`.
4. Physical path: `E:\AllWebApplication\TestVault\Current`.
5. Binding: whatever hostname/port this environment actually uses (e.g.
   `testvault-uat.yourdomain.internal` on port 80/443). This guide
   intentionally doesn't prescribe one — it's environment-specific.
6. Grant the application pool identity (`IIS AppPool\TestVault_UAT` by
   default) **read & execute** on `E:\AllWebApplication\TestVault\Current`
   and everything under it.

### 1.5 SQL Server access

The server IIS runs on (or the app pool's identity, if using a domain
account) needs network access to SQL Server and a login with rights to the
`TestVault` database. Apply the schema once, in either order:

```
sqlcmd -S <server> -d TestVault -i Database\schema.sql
sqlcmd -S <server> -d TestVault -i Database\identity-schema.sql
```

(`schema.sql` is `test_runs`/`test_cases`/`ai_analysis` — hand-written, since
no prior schema existed to derive it from. `identity-schema.sql` is
ASP.NET Core Identity's tables + `RefreshTokens` — genuinely generated via
`dotnet ef migrations script`, not hand-typed; see that file's own header
for how to regenerate it if the Identity model ever changes. Neither
overlaps the other's tables.)

### 1.6 Playwright test-execution directory

`Playwright:WorkingDirectory` (an environment variable, or added to the
workflow's web.config injection alongside the three secrets — see §4) must
point at wherever `playwright.config.js`/`package.json`/the `tests/`
folder actually live on this server, with Node.js, npm, and
`npx playwright install` already run there. This is **not** the IIS
site's own physical path — it's the still-unchanged Node/Playwright side
of this migration (see the "test-execution bridge" decision from Phase 0).

### 1.7 GitHub self-hosted runner

Register a self-hosted Actions runner on this same Windows Server (Settings
→ Actions → Runners → New self-hosted runner in the GitHub repo), running
as a Windows Service under an account that has:

- Local Administrator rights, or specifically enough to manage IIS
  (`IIS_IUSRS`/`Administrators` group membership) — needed for
  `Stop-WebAppPool`/`Start-WebAppPool`.
- Write access to `E:\AllWebApplication\TestVault\*`.
- The .NET 8.0 SDK on `PATH` (this is what `dotnet build`/`publish` runs
  with in the workflow — separate from the Hosting Bundle in §1.1, which
  is what IIS itself uses to *run* the published app).

---

## 2. Folder Structure

```
E:\AllWebApplication\TestVault
│
├── Releases
│   ├── Release_20260914_1000        ← one dotnet publish output per deployment
│   ├── Release_20260914_1100        ← (RELEASES_TO_KEEP = 5, oldest pruned)
│   └── ...
│
├── Current                          ← what IIS's TestVault website actually serves;
│                                        this is the ONLY folder IIS/the app pool
│                                        physical path ever points at
│
└── Backup
    ├── Backup_20260914_1000         ← snapshot of Current, taken immediately
    ├── Backup_20260914_1100         ←   before each deployment overwrites it
    └── ...                            (BACKUPS_TO_KEEP = 5, oldest pruned)
```

Each `Release_*`/`Backup_*` folder is a complete, independent, self-contained
copy of the published app (including its `web.config` with that
deployment's environment variables already baked in — see §4) — nothing is
ever deployed by reference or symlink. Deploying is "stop the pool, replace
`Current`'s contents, start the pool"; rolling back is the same operation
with a `Backup_*` folder as the source instead of a `Release_*` folder.

---

## 3. GitHub Actions Workflow

The complete workflow lives at
[`.github/workflows/testvault-iis-deploy.yml`](../.github/workflows/testvault-iis-deploy.yml) —
read it directly rather than a second copy pasted here, so there's exactly
one source of truth. Its steps, in order:

```
Checkout
   ↓
Analyze Project Structure     (locate the .sln and the Sdk="Microsoft.NET.Sdk.Web" project - never hardcoded)
   ↓
Environment Validation        (dotnet SDK, IIS WebAdministration module, AppPool exists
                                AND is "No Managed Code"/"Integrated", IIS site exists)
   ↓
dotnet restore
   ↓
dotnet build --configuration Release
   ↓
Prepare Deployment Directories
   ↓
Create Release folder         (Releases\Release_<timestamp>)
   ↓
dotnet publish -> Release folder
   ↓
Configure Environment Variables  (injects the 3 secrets into that release's web.config)
   ↓
Backup Current                (snapshot Current -> Backup\Backup_<timestamp>, skipped if Current is empty)
   ↓
Deploy Release                (Stop-WebAppPool -> replace Current's contents -> Start-WebAppPool)
   ↓
Health Check                  (GET /health, GET /api/runs, POST /hubs/run/negotiate)
   ↓
   ├─ success → Cleanup Old Releases, Cleanup Old Backups (keep newest 5 of each)
   └─ failure → Rollback (restore the backup just taken, restart the pool, fail the job)
```

Trigger: **manual only** (`workflow_dispatch`), on purpose — see the
workflow file's own header comment and §5's checklist for why `push: main`
isn't wired up yet.

---

## 4. Configuration Handling (secrets)

Nothing in this repository ever contains a real connection string, JWT
secret, or AI API key — `TestVault.Web/appsettings*.json` ship only empty
placeholders with a `"// NOTE"` explaining that (established back in
Phases 2/3/6, unchanged here). The workflow's **Configure Environment
Variables** step is where real values enter the picture, and only there:

1. Configure three **GitHub Actions repository secrets** (Settings →
   Secrets and variables → Actions → New repository secret):
   - `TESTVAULT_CONNECTION_STRING` → the real SQL Server connection string.
   - `TESTVAULT_JWT_SECRET` → a random string, **32+ characters**
     (`TestVault.Web`'s own startup check refuses to boot otherwise - see
     Program.cs). Generate one with `openssl rand -base64 32` or
     `[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))`
     in PowerShell. **Different secret per environment** (UAT/production) -
     a token issued by one must never validate against another.
   - `TESTVAULT_AI_API_KEY` → the Anthropic API key for AI failure
     analysis. Optional — if omitted, the app still starts and everything
     works except `POST /api/analysis/analyze/{id}`, which fails per-request
     with a 500 (the same graceful-degradation behavior as any other
     misconfigured external dependency in this app).

2. The workflow maps these into the job as `env:` variables on that one
   step (`DEPLOY_CONNECTION_STRING`, `DEPLOY_JWT_SECRET`,
   `DEPLOY_AI_API_KEY`) and writes them into the **just-published release's**
   `web.config`, under `<system.webServer><aspNetCore><environmentVariables>`.
   This is the standard, documented way to pass environment variables to an
   in-process ANCM-hosted app — IIS launches the app with these as real
   process environment variables, and `ConnectionStrings__DefaultConnection`
   (double underscore = ASP.NET Core configuration's own nested-key
   separator) is exactly what `TestVault.Web` already expects.
3. Referencing a secret via `${{ secrets.X }}` anywhere in a job
   automatically registers it for **log masking** — even an accidental
   `Write-Host` of one would show as `***` in the Actions log. The workflow
   doesn't rely on this alone (it never echoes a secret value on purpose
   either), but it's a real safety net.
4. The values persist only in: GitHub's encrypted secret store, this one
   step's process memory during a run, and the deployed `web.config` on
   the server's disk (inside `Releases\<name>\` and `Current\` -
   server-local, never inside the git repository, which stops at
   `E:\AllWebApplication\TestVault`'s absence from any commit).
5. **Restrict filesystem permissions** on `E:\AllWebApplication\TestVault\`
   (Releases/Current/Backup all contain `web.config` with live secrets in
   plaintext) to Administrators + the app pool identity only - anyone else
   with read access to that path can read the connection string and JWT
   signing key directly off disk.

---

## 5. Rollback Strategy

**Automatic** (the common case): the workflow's **Rollback On Failure**
step runs whenever *any* prior step fails (`if: failure()`), most likely
the health check. It:

1. Stops the application pool.
2. Restores `Current` from the `Backup_<timestamp>` folder captured
   immediately before this deployment touched anything.
3. Restarts the application pool.
4. Deliberately still fails the job (`exit 1`) — a rollback means the
   *deployment* failed, and that must stay visible in the Actions run
   history, not be silently swallowed into a green checkmark.

This is why **Backup Current Version** always runs before **Deploy
Release**, unconditionally: there is no rollback without a backup to roll
back to. If `Current` was empty (first-ever deployment), there's nothing to
back up — `OLD_BACKUP` is left empty, and a failure at that point requires
the manual path below (there is no "previous good version" yet by
definition).

**Manual rollback** (if you need to go back further than the immediately
preceding version, or the automatic path itself couldn't run):

```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name "TestVault_UAT"

# Pick the desired backup (or an older Releases\Release_* folder, which is
# equally valid - both are complete, self-contained deployable copies).
$restoreFrom = "E:\AllWebApplication\TestVault\Backup\Backup_20260914_1000"

Get-ChildItem "E:\AllWebApplication\TestVault\Current" -Force | Remove-Item -Recurse -Force
Copy-Item "$restoreFrom\*" "E:\AllWebApplication\TestVault\Current" -Recurse -Force

Start-WebAppPool -Name "TestVault_UAT"
```

Then verify manually: `Invoke-WebRequest http://localhost/health`.

**What rollback does not undo**: a database migration/schema change applied
as part of a release. Nothing in this workflow runs `dotnet ef database
update` or executes SQL against the target database automatically (schema
changes are applied manually, per §1.5) — by design, so that rolling back
the application code is never entangled with rolling back schema, which is
frequently one-directional (e.g. a column that started being written to).
Plan schema changes to be backward-compatible with the previous app version
for at least one release cycle.

---

## 6. Health Check

Three checks, all against the live IIS site (matching this phase's
explicit requirements):

| Check | Request | Pass condition |
|---|---|---|
| Application returns HTTP 200 | `GET /health` | `200` |
| API endpoint reachable | `GET /api/runs` | `200`, **or `401`** |
| SignalR negotiate endpoint reachable | `POST /hubs/run/negotiate` | `200`, **or `401`** |

`/health` is new in this phase (`Program.cs`, `AddHealthChecks()` +
`MapHealthChecks("/health").AllowAnonymous()`) — discovered necessary while
writing this workflow: the only endpoint a naive health check could have
hit otherwise, `GET /`, has nothing mapped to it (`MapControllers`/`MapHub`
only) and would 404 on every single deployment, always, regardless of
whether the app was actually healthy. That bug existed in an earlier draft
of this workflow and is fixed here, not carried forward.

`/api/runs` and `/hubs/run/negotiate` sit behind Phase 6/7's global
authentication requirement, and the health check has no test user's
credentials to present. A **401 counts as healthy** for exactly these two -
it proves the endpoint exists, routing/DI/middleware all ran correctly, and
authentication is being enforced as designed. Only a connection failure, a
5xx, or an unexpected status fails the check.

---

## 7. Production Checklist

**Before the first deployment to a new environment:**

- [ ] .NET 8.0 Hosting Bundle installed (§1.1); `Get-WebGlobalModule -Name AspNetCoreModuleV2` succeeds.
- [ ] `TestVault_UAT` application pool exists: .NET CLR Version = No Managed Code, Pipeline Mode = Integrated (§1.2).
- [ ] Folder structure created; app pool identity has read & execute on `Current\` (§1.3–1.4).
- [ ] `TestVault` IIS site created, bound to `Current\`, with a real binding (§1.4).
- [ ] SQL Server reachable from this server; `Database/schema.sql` and `Database/identity-schema.sql` both applied (§1.5).
- [ ] Node.js, npm, and Playwright browsers (`npx playwright install`) installed at the path `Playwright:WorkingDirectory` will point to (§1.6).
- [ ] Self-hosted GitHub Actions runner registered and running as a service on this server, with the .NET 8.0 **SDK** on `PATH` and rights to manage IIS (§1.7).
- [ ] `TESTVAULT_CONNECTION_STRING`, `TESTVAULT_JWT_SECRET` (32+ chars, unique per environment), and optionally `TESTVAULT_AI_API_KEY` set as GitHub Actions secrets on this repository (§4).
- [ ] Filesystem permissions on `E:\AllWebApplication\TestVault\` restricted to Administrators + the app pool identity (§4).

**Every deployment:**

- [ ] Run the workflow manually (`workflow_dispatch`) first, watch it complete, and manually verify the app (`/health`, log in through the frontend, start a manual run) before trusting it unattended.
- [ ] Check the Actions run log for the rollback step firing — a rollback means something is genuinely wrong even though the job failed "safely".
- [ ] Confirm `Releases\` and `Backup\` aren't unexpectedly growing past `RELEASES_TO_KEEP`/`BACKUPS_TO_KEEP` (5 each) - the cleanup steps only run `if: success()`, so a string of failed deployments accumulates uncleaned releases.

**Before flipping this on `push: main` (retiring the Node.js workflow for good):**

- [ ] At least one full manual run of `testvault-iis-deploy.yml` has succeeded against this exact server, including a real login/dashboard/manual-run/logout pass through the deployed app (not just the automated health check).
- [ ] `.github/workflows/testVault Deploy.yml` (the Node.js/NSSM-based deployment this replaces) is disabled or removed, and its NSSM-managed backend Windows Service is stopped - running both deployment paths against the same IIS site at once will corrupt each other's `Current\` folder.
- [ ] The React frontend has its own deployment plan for this environment (out of scope here - see this file's own Scope note).
- [ ] Someone has logged in as the bootstrap admin account (its one-time random password is only ever shown in the application's own startup log - see `IdentitySeeder`), and either changed its password or provisioned real accounts via `POST /api/admin/users` and retired it.
- [ ] Uncomment the `push: branches: [main]` trigger in `testvault-iis-deploy.yml`.
