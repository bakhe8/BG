# Production Release Handoff

## Release Identity

- Environment:
- Release date:
- Deployment window:
- Deployment operator:
- Database operator:
- Application owner:

## Build Provenance

- Branch:
- Commit:
- Publish folder:
- Release archive:
- Release manifest:

## Package Integrity

- `BG.Web.dll` SHA256:
- `web.config` SHA256:
- `OcrWorker\ocr_worker.py` SHA256:
- Release archive SHA256:

## Required Server Inputs

- Public host name:
- TLS certificate thumbprint:
- `ConnectionStrings__PostgreSql` supplied:
- `Storage__DocumentsRoot` supplied:
- `DataProtection__KeysPath` supplied:
- `AllowedHosts` supplied:
- `OperationalSeed__Enabled=false` confirmed:
- `Swagger__Enabled=false` confirmed:
- `Ocr__Enabled` approved for this environment:

## Migration Plan

- Migration mode:
  - manual `dotnet-ef`
  - controlled startup migration
- Database backup completed:
- Rollback database backup location:

## Deployment Paths

- IIS site root:
- IIS backup root:
- Documents root:
- Data-protection keys root:

## Validation Commands

- Release build:
- Release tests:
- Server environment check:
- Health check URL:

## Handoff Status

- Package reviewed:
- Server checklist completed:
- Smoke sign-off completed:
- Ready for go-live:

## Notes

- 
