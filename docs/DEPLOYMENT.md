# TestVault - Production Deployment Guide

Phase 8 of the Node.js → ASP.NET Core migration. This document is the
human-facing companion to
[`.github/workflows/testvault-iis-deploy.yml`](../.github/workflows/testvault-iis-deploy.yml) —
read this before the first deployment, and keep it updated if the workflow
changes.

**Scope**: this covers deploying `TestVault.Web` (the ASP.NET Core API) to
IIS. The React frontend (`frontend/`) is a separate static site with its own
deployment story, out of scope here. For running `TestVault.Web` on a
developer machine instead, see [`DEVELOPMENT.md`](DEVELOPMENT.md).

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

Name: **`TestVault`** (matches `IIS_APP_POOL` in the workflow — if you
use a different name, update the workflow's `env:` block to match).

Via IIS Manager:

1. Application Pools → Add Application Pool.
2. Name: `TestVault`.
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
New-WebAppPool -Name "TestVault"
Set-ItemProperty "IIS:\AppPools\TestVault" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\TestVault" -Name managedPipelineMode -Value "Integrated"
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
3. Application pool: `TestVault`.
4. Physical path: `E:\AllWebApplication\TestVault\Current`.
5. Binding: whatever hostname/port this environment actually uses (e.g.
   `testvault-uat.yourdomain.internal` on port 80/443). This guide
   intentionally doesn't prescribe one — it's environment-specific.
   **If you bind to a specific hostname** (not blank/catch-all), IIS's
   built-in **Default Web Site** still has its own catch-all binding on
   port 80 — a request without the right `Host` header (e.g. a bare
   `http://localhost/...` probe) lands on Default Web Site instead of this
   one, and 404s on every route since Default Web Site's physical path has
   none of them. The workflow's **Environment Validation** step reads this
   site's actual binding and has **Application Health Check** send the
   matching `Host` header automatically (§6) — no manual step needed here,
   just be aware of it if you ever test with `curl`/a browser directly.
6. Grant the application pool identity (`IIS AppPool\TestVault` by
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

- **Local Administrator rights** — see §1.8. This is the one prerequisite
  that's easy to get wrong: `IIS_IUSRS` membership does **not** substitute
  for it, however it might look like it should.
- Write access to `E:\AllWebApplication\TestVault\*`.
- The .NET 8.0 SDK on `PATH` (this is what `dotnet build`/`publish` runs
  with in the workflow — separate from the Hosting Bundle in §1.1, which
  is what IIS itself uses to *run* the published app).

### 1.8 IIS Application Pool management permissions

The workflow's **Deploy Release** and **Rollback On Failure** steps stop
and start the `TestVault` Application Pool (via `appcmd.exe`, calling into
WAS — the Windows Process Activation Service). That specifically requires
the runner's service account to be a **local Administrator** on this
server. Two things that look like they should be enough, but aren't:

- **`IIS_IUSRS` group membership** — this is the built-in group IIS puts
  an application pool's *worker process identity* into (the account the
  app itself runs as). It carries no rights to *manage* a pool's state.
- **NTFS permissions on `E:\AllWebApplication\TestVault\`** — this only
  covers the site's content files. Stopping/starting a pool goes through
  `applicationHost.config` and WAS, neither of which lives under that path.

Set it up:

```powershell
# Run as a real local Administrator, not as the runner's own account.
Add-LocalGroupMember -Group "Administrators" -Member "CICD"

# If the runner's account is a LOCAL (non-domain) account, also disable
# UAC's remote-token filtering for local admins - otherwise Windows can
# silently downgrade that account's token to "standard user" for the kind
# of loopback/COM call Stop-WebAppPool/appcmd's underlying WAS calls make,
# even though the account genuinely is an Administrator. This is the
# classic cause of "works when I run it manually, Access is Denied when
# the same account runs it as a service."
New-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
    -Name "LocalAccountTokenFilterPolicy" -PropertyType DWord -Value 1 -Force

# Restart the runner service so it picks up a token reflecting the new
# group membership - a running service keeps the token it started with,
# so this step is required, not optional, after the change above.
Get-Service "actions.runner.*" | Restart-Service
```

Verify by running as the runner's own account (not your own, possibly more
privileged, interactive session) — `runas /user:CICD "powershell -NoExit
-Command Stop-WebAppPool -Name TestVault; Start-WebAppPool -Name TestVault"`
should succeed with no prompt beyond the account's password.

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
Configure Environment Variables  (sets ASPNETCORE_ENVIRONMENT=Production in that
                                   release's web.config; reports whether the server-side
                                   Application Pool config secrets require is present)
   ↓
Backup Current                (snapshot Current -> Backup\Backup_<timestamp>, skipped if Current is empty)
   ↓
Deploy Release                (appcmd stop apppool -> replace Current's contents -> appcmd start apppool)
   ↓
Health Check                  (GET /health, GET /api/runs, POST /hubs/run/negotiate -
                                against this site's OWN IIS binding, Host header included - §6.1)
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
Phases 2/3/6, unchanged here).

Deployment (GitHub Actions) and application secret management (this
server) are two deliberately separate concerns:

```
   Old (removed)                      Current

GitHub Secrets                  IIS Server Environment Variables
      |                                    |
      v                                    v
   Deployment                    ASP.NET Core Configuration
      |                                    |
      v                                    v
  web.config                          Application
```

The old path meant every deployment depended on GitHub Secrets being
present just to move code — a secret rotation, a missing repo secret, or a
CI-side masking quirk could all block a deployment that had nothing to do
with application configuration. The workflow's job is to ship code
([.github/workflows/testvault-iis-deploy.yml](../.github/workflows/testvault-iis-deploy.yml):
checkout, build, publish, backup, deploy, restart the App Pool, health
check) — not to be a secret store. `TestVault.Web`'s configuration is this
server's responsibility, independent of any given deployment.

**Production values are configured once on the server, not in GitHub.**
This repo does **not** use GitHub Actions secrets for
`ConnectionStrings__DefaultConnection`, `Jwt__Secret`, or `AI__ApiKey` —
deliberately: `dotnet publish` regenerates a brand-new `web.config` on
*every single deployment* (see the **Publish Application** step), so
anything written into it wouldn't survive past the next release anyway,
and secrets sitting in plaintext inside `Releases\*\web.config` on disk are
harder to lock down than the alternative below. A value that must persist
across deployments has to live somewhere `dotnet publish` never touches:
the **IIS Application Pool itself**.

### 4.1 One-time setup: Application Pool environment variables

IIS 10.0 / Windows Server 2016+ supports environment variables scoped to a
single Application Pool. The ASP.NET Core Module (ANCM) launches `w3wp.exe`
with these already present in its process environment, so `TestVault.Web`
picks them up exactly the same way it would from `web.config`'s own
`<environmentVariables>` — no code or web.config change needed.

```powershell
Import-Module WebAdministration

$pool = "TestVault"   # match IIS_APP_POOL in the workflow

foreach ($kv in @{
    'ConnectionStrings__DefaultConnection' = '<real SQL Server connection string>'
    'Jwt__Secret'                          = '<random, 32+ characters>'
    'AI__ApiKey'                           = '<Anthropic API key - optional>'
}.GetEnumerator()) {
    Add-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
        -Filter "system.applicationHost/applicationPools/add[@name='$pool']/environmentVariables" `
        -Name "." -Value @{ name = $kv.Key; value = $kv.Value }
}

Restart-WebAppPool -Name $pool
```

Equivalent GUI path: **IIS Manager → Application Pools → select the pool →
Configuration Editor** (top-right link) → section
`system.applicationHost/applicationPools` → locate this pool's entry →
`environmentVariables` → **Add**.

Generate `Jwt__Secret` with `openssl rand -base64 32` or
`[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))`
in PowerShell — **32+ characters**, `TestVault.Web`'s own startup check
(`Program.cs`) refuses to boot otherwise. Use a **different secret per
environment** (UAT/production) so a token issued by one can never validate
against another. `AI__ApiKey` is optional — if omitted, the app still
starts and everything works except `POST /api/analysis/analyze/{id}`, which
fails per-request with a 500 (the same graceful-degradation behavior as any
other misconfigured external dependency in this app).

**To rotate a value later**: re-run the `Add-WebConfigurationProperty` call
for just that key with the new value (or edit it via Configuration Editor),
then `Restart-WebAppPool`. This is independent of the deployment pipeline —
no code change, no workflow run, no GitHub secret to update.

### 4.2 What the workflow does with this

The **Configure Environment Variables** step only ever writes
`ASPNETCORE_ENVIRONMENT=Production` into the newly published release's
`web.config` (not a secret, identical for every deployment — this is what
makes `TestVault.Web` load `appsettings.Production.json` at all). It then
reads back `IIS:\AppPools\<pool>\environmentVariables` and logs, per key,
whether `ConnectionStrings__DefaultConnection` / `Jwt__Secret` /
`AI__ApiKey` are present — **informational only, never blocking**. If §4.1
was skipped, the deployment still proceeds; `TestVault.Web` will fail its
own startup check (missing `Jwt:Secret`) or fail to reach the database, the
**Health Check** step will catch that, and **Rollback On Failure** restores
the previous working release automatically (§5). The step's log is there so
the actual cause — a missing Application Pool environment variable — is
visible immediately, rather than only inferable from a failed health check.

### 4.3 Filesystem permissions

Restrict filesystem permissions on `E:\AllWebApplication\TestVault\` to
Administrators + the app pool identity only. `web.config` in
`Releases\`/`Current\`/`Backup\` no longer carries secrets, but it's still
part of the running application and shouldn't be world-readable.

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
Stop-WebAppPool -Name "TestVault"

# Pick the desired backup (or an older Releases\Release_* folder, which is
# equally valid - both are complete, self-contained deployable copies).
$restoreFrom = "E:\AllWebApplication\TestVault\Backup\Backup_20260914_1000"

Get-ChildItem "E:\AllWebApplication\TestVault\Current" -Force | Remove-Item -Recurse -Force
Copy-Item "$restoreFrom\*" "E:\AllWebApplication\TestVault\Current" -Recurse -Force

Start-WebAppPool -Name "TestVault"
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

### 6.1 How the target URL/Host header is chosen

**Never hardcoded.** A fixed `http://localhost/...` assumption silently
breaks the moment the IIS site is bound to a specific hostname instead of a
blank/catch-all binding (§1.4) — the request then lands on IIS's Default
Web Site instead of `TestVault`, and every route 404s even though the app
and Application Pool are both completely healthy. To avoid that:

1. **Environment Validation** reads the `TestVault` site's actual
   `physicalPath` (fails the deployment early if it doesn't match
   `Current\` — a second, independent way this exact symptom can happen)
   and its first `http` binding, exporting the binding's port and host
   header (if any) as `HEALTH_CHECK_PORT` / `HEALTH_CHECK_HOST_HEADER`.
2. **Application Health Check** builds its request against
   `http://localhost[:port]` and, if the binding required one, sends the
   matching `Host` header explicitly on every request.
3. Passing `health_url` to `workflow_dispatch` still overrides all of this
   — useful for testing against a real external hostname/HTTPS binding
   directly, bypassing the loopback/Host-header logic entirely.

**Final health check URLs actually used** (both are correct — which one
applies depends purely on how the site is bound, checked automatically
every run):

- Blank/catch-all binding on port 80: `http://localhost/health`,
  `http://localhost/api/runs`, `http://localhost/hubs/run/negotiate` (no
  `Host` header override — this is what it fell back to before, and remains
  correct for a site actually bound that way).
- Specific-hostname binding, e.g. `testvault-uat.yourdomain.internal` on
  port 80: same paths, same `http://localhost` request line (still the
  loopback address — this is the same server), but with
  `Host: testvault-uat.yourdomain.internal` sent explicitly so IIS routes
  to `TestVault` instead of Default Web Site.

---

## 7. Production Checklist

**Before the first deployment to a new environment:**

- [ ] .NET 8.0 Hosting Bundle installed (§1.1); `Get-WebGlobalModule -Name AspNetCoreModuleV2` succeeds.
- [ ] `TestVault` application pool exists: .NET CLR Version = No Managed Code, Pipeline Mode = Integrated (§1.2).
- [ ] Folder structure created; app pool identity has read & execute on `Current\` (§1.3–1.4).
- [ ] `TestVault` IIS site created, bound to `Current\`, with a real binding (§1.4) — Environment Validation now verifies both automatically every run, but confirm once by hand too: `(Get-Website -Name TestVault).physicalPath` and `Get-WebBinding -Name TestVault`.
- [ ] SQL Server reachable from this server; `Database/schema.sql` and `Database/identity-schema.sql` both applied (§1.5).
- [ ] Node.js, npm, and Playwright browsers (`npx playwright install`) installed at the path `Playwright:WorkingDirectory` will point to (§1.6).
- [ ] Self-hosted GitHub Actions runner registered and running as a service on this server, with the .NET 8.0 **SDK** on `PATH` (§1.7).
- [ ] Runner's service account is a **local Administrator** on this server (not just `IIS_IUSRS`), and `LocalAccountTokenFilterPolicy` is set if it's a local account (§1.8) — verify with `runas /user:<account> "powershell -Command Stop-WebAppPool -Name TestVault"`.
- [ ] `ConnectionStrings__DefaultConnection` and `Jwt__Secret` (32+ chars, unique per environment) set as environment variables on the `TestVault` Application Pool, and `AI__ApiKey` set if AI failure analysis is used (§4.1). No GitHub Actions secrets are used for these.
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
