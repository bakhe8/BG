# Local Development

## PostgreSQL

BG uses a dedicated local PostgreSQL database and reads it from `user-secrets` for `BG.Web`.

Verify the local setup at any time:

```powershell
.\scripts\Test-BgLocalEnvironment.ps1
```

Expected local prerequisites:

- `ConnectionStrings:PostgreSql` exists in `dotnet user-secrets --project src/BG.Web`
- `Storage:DocumentsRoot` exists in `dotnet user-secrets --project src/BG.Web`
- `Identity:BootstrapAdmin:Password` exists in `dotnet user-secrets --project src/BG.Web`
- PostgreSQL service is running
- Database migrations have been applied

Set the required secrets locally:

```powershell
dotnet user-secrets --project src/BG.Web set "ConnectionStrings:PostgreSql" "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=your-local-password"
dotnet user-secrets --project src/BG.Web set "Storage:DocumentsRoot" "C:\\BG\\documents"
dotnet user-secrets --project src/BG.Web set "Identity:BootstrapAdmin:Password" "your-local-admin-password"
```

If you need the seeded operator/demo pack locally, enable it explicitly through `user-secrets` rather than keeping it in tracked configuration:

```powershell
dotnet user-secrets --project src/BG.Web set "OperationalSeed:Enabled" "true"
dotnet user-secrets --project src/BG.Web set "OperationalSeed:SharedPassword" "your-local-seed-password"
```

Re-apply migrations locally:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/BG.Infrastructure --startup-project src/BG.Web
```

Run the web app locally:

```powershell
dotnet run --project src/BG.Web
```

## OCR Local Setup

`OCR` is part of the current local development baseline. The tracked `Development`
settings expect a local Python virtual environment at `.venv-ocr312`.

Create and populate it:

```powershell
py -3.12 -m venv .venv-ocr312
.venv-ocr312\Scripts\python.exe -m pip install --upgrade pip
.venv-ocr312\Scripts\python.exe -m pip install -r src\BG.Integrations\OcrWorker\requirements-wave1.txt
```

The local `Development` configuration points to:

- `.venv-ocr312\Scripts\python.exe`
- `src\BG.Integrations\OcrWorker\ocr_worker.py`

Verify the OCR worker path through the mandatory integration tests:

```powershell
dotnet test tests\BG.UnitTests\BG.UnitTests.csproj --filter FullyQualifiedName~LocalPythonOcrProcessingServiceTests
```

Notes:

- Local development must run with `ASPNETCORE_ENVIRONMENT=Development` so that `user-secrets` are loaded.
- `OperationalSeed` is disabled by default in tracked configuration. Enable it only through local secrets when you explicitly need seeded users and data.
- `OCR` tests are mandatory now. If `.venv-ocr312` or the worker script is missing, the test suite should fail instead of silently skipping OCR coverage.
- Production / IIS must provide `ConnectionStrings:PostgreSql` through environment variables or site configuration.
- The project is intentionally configured to fail fast with a clear error if the PostgreSQL password is still the placeholder `change-me`.
