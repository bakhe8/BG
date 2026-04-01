# Production Server Checklist

Use this checklist before the first production deployment and before every
go-live.

Related documents:

- [PRODUCTION_DEPLOYMENT.md](../PRODUCTION_DEPLOYMENT.md)
- [PRODUCTION_RUNBOOK.md](../PRODUCTION_RUNBOOK.md)
- [web.config.environmentVariables.example.xml](web.config.environmentVariables.example.xml)

## 1. Server Identity

- Server name:
- Environment:
- Public host name:
- TLS certificate thumbprint:
- Deployment operator:
- Database operator:
- Go-live window:

## 2. Host Baseline

- `IIS` installed
- `.NET 8 Hosting Bundle` installed
- `Microsoft.AspNetCore.App 8.x` verified
- Site folder created: `C:\inetpub\BG`
- Backup folder created: `C:\inetpub\BG_backups`
- Documents folder created: `C:\BG\documents`
- Data-protection folder created: `C:\BG\data-protection`
- App pool identity confirmed
- HTTPS binding created
- Real certificate bound to the production host name

## 3. Configuration Inputs

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__PostgreSql` filled with the real production value
- `Storage__DocumentsRoot` filled with the persistent path
- `DataProtection__KeysPath` filled with the persistent path
- `AllowedHosts` filled with the explicit production host list
- `ReverseProxy__KnownProxies__*` or `ReverseProxy__KnownNetworks__*` filled with the actual trusted edge topology
- `OperationalSeed__Enabled=false`
- `Swagger__Enabled=false`
- `Ocr__Enabled=false` or explicitly approved for this environment

### Optional Bootstrap Admin

- Temporary bootstrap admin needed:
- Username:
- Password delivery method:
- Removal owner after first sign-in:

### Optional OCR

- OCR enabled for this environment:
- Python executable path:
- Worker script path:
- Timeout seconds:
- Max file size bytes:
- Queue capacity:
- Local OCR dependencies verified:

## 4. Database And Storage

- PostgreSQL connectivity verified from the web server
- Database backup completed before deployment
- Current deployment folder backup completed before deployment
- Migration mode selected:
  - manual `dotnet-ef`
  - controlled startup migration
- Document root write access verified for the app pool identity
- Data-protection key path write access verified for the app pool identity

## 5. Release Package

- Release package built from the intended branch
- Commit SHA:
- Publish folder path:
- Zip package path:
- `release-manifest.json` reviewed
- `web.config` updated with the intended environment variables

## 6. Pre-Start Validation

- `.\scripts\Test-BgProductionEnvironment.ps1 -SiteRoot 'C:\inetpub\BG'` passed
- No placeholder password remains in the connection string
- No wildcard `*` remains in `AllowedHosts`
- Reverse proxy trust values match the real proxy IPs or networks
- `OperationalSeed` confirmed disabled
- `Swagger` confirmed disabled

## 7. Post-Start Validation

- `GET /health` returned `Healthy`
- Sign-in succeeded
- `/Requests/Workspace` loaded
- `/Intake/Workspace` loaded
- `/Operations/Queue` loaded
- `/Approvals/Queue` loaded
- `/Dispatch/Workspace` loaded
- `/Administration/Users` loaded
- Document staging wrote to the persistent documents path
- If OCR is enabled, at least one OCR request succeeded

## 8. Post-Deploy Cleanup

- Temporary bootstrap admin password removed from live configuration
- Backups confirmed
- Deployment logs archived
- Rollback location recorded

## 9. Sign-Off

- Deployment operator sign-off:
- Database operator sign-off:
- Application owner sign-off:
- Date:
