# Production Deployment

## Document Role

- Status: `source of truth`
- Scope: production configuration baseline and runtime guards
- Documentation index: [README.md](../README.md)
- Repository-wide testing policy: [testing.instructions.md](../../.github/instructions/testing.instructions.md)
- Execution procedure: [PRODUCTION_RUNBOOK.md](./PRODUCTION_RUNBOOK.md)

This document defines the minimum deployment baseline for running `BG` as a production workload.

For the step-by-step execution procedure, use [PRODUCTION_RUNBOOK.md](./PRODUCTION_RUNBOOK.md).

## Boundary

This document defines what production must satisfy, not the full step-by-step deployment procedure.

- Use this file for required production settings, runtime guards, and host baseline.
- Use [PRODUCTION_RUNBOOK.md](./PRODUCTION_RUNBOOK.md) for the exact deployment sequence.
- Use [testing.instructions.md](../../.github/instructions/testing.instructions.md) for the repository-wide build and test quality gate.

## Required Configuration

Do not rely on repository defaults in production. Configure these values through IIS environment variables, machine-level configuration, or your secrets management process.

- `ConnectionStrings:PostgreSql`
  A real PostgreSQL connection string. Placeholder passwords are rejected at startup.
- `Storage:DocumentsRoot`
  Absolute path to the persisted document volume for staged and promoted guarantee files.
- `DataProtection:KeysPath`
  Absolute path to persisted ASP.NET Core data-protection keys.
- `AllowedHosts`
  Explicit host list. Wildcard `*` is rejected in production.
- `ReverseProxy:KnownProxies` or `ReverseProxy:KnownNetworks`
  Explicit trusted reverse-proxy addresses or CIDR ranges for forwarded-header processing.

## Optional Configuration

- `Identity:BootstrapAdmin:*`
  Only configure for controlled first-run bootstrap or break-glass access.
- `Identity:LoginLockout:*`
  Override only when the environment requires thresholds different from the built-in lockout defaults.
- `Ocr:*`
  Leave `Ocr:Enabled=false` unless the production node has the Python runtime, worker script, and model dependencies installed locally. When enabled, configure `Ocr:PythonExecutablePath`, `Ocr:WorkerScriptPath`, `Ocr:TimeoutSeconds`, `Ocr:MaxFileSizeBytes`, and `Ocr:QueueCapacity`.

## Production Guards

When `ASPNETCORE_ENVIRONMENT=Production`, the application now fails fast if any of the following are true:

- `AllowedHosts` is blank or contains `*`
- `Storage:DocumentsRoot` is not configured
- `DataProtection:KeysPath` is not configured
- neither `ReverseProxy:KnownProxies` nor `ReverseProxy:KnownNetworks` is configured
- `OperationalSeed:Enabled=true`
- `Swagger:Enabled=true`
- `Ocr:Enabled=true` while the configured Python executable or worker script is missing
- `Ocr:Enabled=true` while `Ocr:MaxFileSizeBytes` or `Ocr:QueueCapacity` is non-positive

## Recommended IIS / Host Baseline

- Terminate TLS before the application and serve only over HTTPS.
- Preserve `X-Forwarded-For` and `X-Forwarded-Proto`.
- Configure only the proxy IPs or networks that are actually allowed to forward headers to the app.
- Persist the documents root and data-protection keys outside the deployment directory.
- Run database migrations before or during startup only under controlled deployment procedures.
- Keep OCR worker dependencies on the same host when OCR is enabled.
- Operational demo data is no longer generated from normal startup. It can run only through the explicit seed command outside production.
- Use the server readiness checklist in [production_server_checklist.md](deployment/production_server_checklist.md) before each deployment.

## Smoke Verification

This section defines the minimum production acceptance baseline only. Use [PRODUCTION_RUNBOOK.md](./PRODUCTION_RUNBOOK.md) for the executable smoke sequence.

After deployment, verify:

1. `GET /health` returns `Healthy`.
2. Sign-in succeeds for the intended production admin path.
3. `Requests`, `Intake`, `Operations`, `Approvals`, `Dispatch`, and `Administration` load over HTTPS.
4. Document staging writes to the configured `Storage:DocumentsRoot`.
5. OCR requests succeed only when the local runtime is actually installed.
