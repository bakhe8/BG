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
- PostgreSQL service is running
- Database migrations have been applied

Re-apply migrations locally:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/BG.Infrastructure --startup-project src/BG.Web
```

Run the web app locally:

```powershell
dotnet run --project src/BG.Web
```

Notes:

- Local development must run with `ASPNETCORE_ENVIRONMENT=Development` so that `user-secrets` are loaded.
- Production / IIS must provide `ConnectionStrings:PostgreSql` through environment variables or site configuration.
- The project is intentionally configured to fail fast with a clear error if the PostgreSQL password is still the placeholder `change-me`.
