# Production Runbook

## Document Role

- Status: `source of truth`
- Scope: step-by-step production deployment execution procedure
- Documentation index: [README.md](../README.md)
- Baseline policy document: [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md)
- Repository-wide testing policy: [testing.instructions.md](../../.github/instructions/testing.instructions.md)

This runbook turns the production baseline into an executable deployment procedure for `BG`.

Use it together with [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md).

## Boundary

This document owns the deployment sequence itself.

- Use [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) for the required production baseline and runtime guards.
- Use [testing.instructions.md](../../.github/instructions/testing.instructions.md) for the canonical repository-wide test policy.
- The commands in this runbook operationalize a release; they do not replace the testing policy document.

## 1. Scope

This runbook assumes:

- Host OS: `Windows Server`
- Web host: `IIS`
- Runtime: `.NET 8`
- Database: `PostgreSQL`
- App: `src/BG.Web`

## 2. Release Workstation

Run these commands from the repository root before touching the production server:

```powershell
dotnet tool restore
dotnet build BG.sln -c Release
dotnet test BG.sln -c Release --no-build
dotnet publish src\BG.Web\BG.Web.csproj -c Release -o .\.artifacts\publish\BG.Web
```

Or use the repository script:

```powershell
.\scripts\Publish-BgProductionRelease.ps1 -ZipPackage
```

Expected output:

- publish folder: `.artifacts\publish\BG.Web`
- `web.config` generated for IIS
- application files under `.artifacts\publish\BG.Web`
- OCR worker copied to `.artifacts\publish\BG.Web\OcrWorker\ocr_worker.py`
- `release-manifest.json` generated under `.artifacts\publish\BG.Web`
- handoff package generated under `.artifacts\publish\BG.Web\_handoff`

## 3. Server Prerequisites

Before the first deployment, ensure the server has:

- `IIS` installed
- `.NET 8 Hosting Bundle` installed
- network access to the production PostgreSQL instance
- a real TLS certificate for the production host name

Verify the runtime:

```powershell
dotnet --list-runtimes
```

## 4. Prepare Server Paths

Create persistent paths outside the deployment folder:

```powershell
New-Item -ItemType Directory -Force -Path 'C:\BG\documents' | Out-Null
New-Item -ItemType Directory -Force -Path 'C:\BG\data-protection' | Out-Null
New-Item -ItemType Directory -Force -Path 'C:\inetpub\BG' | Out-Null
New-Item -ItemType Directory -Force -Path 'C:\inetpub\BG_backups' | Out-Null
```

These paths map to:

- `Storage:DocumentsRoot` -> `C:\BG\documents`
- `DataProtection:KeysPath` -> `C:\BG\data-protection`
- deployment root -> `C:\inetpub\BG`

## 5. Create IIS App Pool and Site

Example PowerShell setup:

```powershell
Import-Module WebAdministration

if (-not (Test-Path IIS:\AppPools\BG)) {
    New-WebAppPool -Name 'BG'
}

Set-ItemProperty IIS:\AppPools\BG -Name managedRuntimeVersion -Value ''
Set-ItemProperty IIS:\AppPools\BG -Name processModel.identityType -Value ApplicationPoolIdentity

if (-not (Test-Path IIS:\Sites\BG)) {
    New-Website -Name 'BG' -PhysicalPath 'C:\inetpub\BG' -Port 80 -HostHeader 'bg.example.local' -ApplicationPool 'BG'
}

if (-not (Get-WebBinding -Name 'BG' -Protocol 'https' -ErrorAction SilentlyContinue)) {
    New-WebBinding -Name 'BG' -Protocol https -Port 443 -HostHeader 'bg.example.local'
}
```

Bind the real certificate after replacing the thumbprint:

```powershell
Get-Item "cert:\LocalMachine\My\<CERT_THUMBPRINT>" | New-Item "IIS:\SslBindings\0.0.0.0!443!bg.example.local"
```

## 6. Configure Production Settings

`BG` rejects incomplete production settings at startup. Configure these values before the first start:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__PostgreSql`
- `Storage__DocumentsRoot`
- `DataProtection__KeysPath`
- `AllowedHosts`
- `OperationalSeed__Enabled=false`
- `Swagger__Enabled=false`

### Recommended: Site-level environment variables in `web.config`

After `dotnet publish`, edit the generated `web.config` in the publish folder and add values under `<aspNetCore><environmentVariables>`:

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="ConnectionStrings__PostgreSql" value="Host=db01;Port=5432;Database=bg_prod;Username=bg_prod;Password=REPLACE_ME;Application Name=BG" />
  <environmentVariable name="Storage__DocumentsRoot" value="C:\BG\documents" />
  <environmentVariable name="DataProtection__KeysPath" value="C:\BG\data-protection" />
  <environmentVariable name="AllowedHosts" value="bg.example.local" />
  <environmentVariable name="OperationalSeed__Enabled" value="false" />
  <environmentVariable name="Swagger__Enabled" value="false" />
  <environmentVariable name="Ocr__Enabled" value="false" />
</environmentVariables>
```

You can start from the ready template in [deploy/iis/web.config.environmentVariables.example.xml](../../deploy/iis/web.config.environmentVariables.example.xml).

Before go-live, fill the server handoff checklist in [production_server_checklist.md](deployment/production_server_checklist.md).
The publish output also includes a ready handoff folder with the checklist,
environment variable template, release handoff sheet, and post-deploy smoke
record.

### Optional: Temporary bootstrap admin

Use only for controlled first-run access or break-glass:

```xml
<environmentVariable name="Identity__BootstrapAdmin__Username" value="admin.prod" />
<environmentVariable name="Identity__BootstrapAdmin__DisplayName" value="Production Admin" />
<environmentVariable name="Identity__BootstrapAdmin__Email" value="admin@example.local" />
<environmentVariable name="Identity__BootstrapAdmin__Password" value="REPLACE_WITH_STRONG_PASSWORD" />
```

After the first successful sign-in, remove the bootstrap password from production configuration.

### Optional: OCR

Leave OCR disabled unless the production node has the local Python runtime and the worker dependencies installed.

If OCR is enabled, also configure:

```xml
<environmentVariable name="Ocr__Enabled" value="true" />
<environmentVariable name="Ocr__PythonExecutablePath" value="C:\BG\ocr\.venv\Scripts\python.exe" />
<environmentVariable name="Ocr__WorkerScriptPath" value="OcrWorker\ocr_worker.py" />
<environmentVariable name="Ocr__TimeoutSeconds" value="120" />
```

## 7. Database Migration

Preferred path: run migrations in a controlled step before restarting the IIS site.

`BG` will also run `MigrateAsync()` on startup for PostgreSQL, but treat that as a safety net, not the primary release step.

### Option A: Run migrations from the release workstation

```powershell
$env:ConnectionStrings__PostgreSql = 'Host=db01;Port=5432;Database=bg_prod;Username=bg_prod;Password=REPLACE_ME;Application Name=BG'
dotnet tool restore
dotnet tool run dotnet-ef database update --project src\BG.Infrastructure --startup-project src\BG.Web
Remove-Item Env:ConnectionStrings__PostgreSql
```

### Option B: Allow startup migration during maintenance

If you cannot run the manual migration step, the application will attempt to apply pending PostgreSQL migrations on startup. Only do this during an approved maintenance window.

## 8. Backup Before Deployment

Before copying the new release:

- take a PostgreSQL backup
- backup the current IIS deployment folder
- record the release commit and package details from `release-manifest.json`

Example folder backup:

```powershell
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
Copy-Item 'C:\inetpub\BG' "C:\inetpub\BG_backups\BG-$stamp" -Recurse
```

## 9. Deploy Release Files

Stop the site and app pool:

```powershell
Stop-Website -Name 'BG'
Stop-WebAppPool -Name 'BG'
```

Copy the published release:

```powershell
robocopy .\.artifacts\publish\BG.Web C:\inetpub\BG /E
```

Start the app pool and site:

```powershell
Start-WebAppPool -Name 'BG'
Start-Website -Name 'BG'
```

## 10. Startup Verification

Verify the health endpoint:

```powershell
curl.exe -k https://bg.example.local/health
```

Expected result:

- HTTP `200`
- response contains `"status":"Healthy"`

If startup fails, check:

- Windows Event Viewer
- IIS logs
- the application stdout log if enabled
- missing production settings blocked by `ProductionReadinessValidator`

You can also run the server-side prerequisite checker after setting environment variables and copying the release:

```powershell
.\scripts\Test-BgProductionEnvironment.ps1 -SiteRoot 'C:\inetpub\BG'
```

## 11. Functional Smoke Test

Run this smoke checklist immediately after deployment:

1. Open the sign-in page over `HTTPS`.
2. Sign in with the intended production admin path.
3. Open:
   - `/Requests/Workspace`
   - `/Intake/Workspace`
   - `/Operations/Queue`
   - `/Approvals/Queue`
   - `/Dispatch/Workspace`
   - `/Administration/Users`
4. Upload or stage a document and confirm the file is written under `Storage:DocumentsRoot`.
5. If OCR is enabled, confirm at least one OCR request succeeds.

## 12. Post-Deploy Actions

After the application is stable:

- remove bootstrap admin password from live configuration if it was used
- confirm backups succeeded
- confirm `OperationalSeed` is still disabled
- confirm `Swagger` is still disabled
- confirm `AllowedHosts` is explicit and does not contain `*`

## 13. Rollback

If the deployment must be rolled back:

1. Stop the IIS site and app pool.
2. Restore the previous folder backup to `C:\inetpub\BG`.
3. Start the app pool and site.
4. If the new release applied a non-compatible database migration, restore the PostgreSQL backup as well.

Example file rollback:

```powershell
Stop-Website -Name 'BG'
Stop-WebAppPool -Name 'BG'
robocopy C:\inetpub\BG_backups\BG-<STAMP> C:\inetpub\BG /E
Start-WebAppPool -Name 'BG'
Start-Website -Name 'BG'
```

## 14. Completion Criteria

The production deployment is complete only when all of the following are true:

- application starts under `Production`
- `/health` returns `Healthy`
- sign-in works
- the six operational/admin surfaces load over `HTTPS`
- document storage writes to the configured persistent path
- no development-only settings remain enabled
