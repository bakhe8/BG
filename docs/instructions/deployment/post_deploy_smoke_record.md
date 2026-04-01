# Post-Deploy Smoke Record

## Deployment Identity

- Environment:
- Host name:
- Deployment date:
- Deployment operator:
- Application verifier:

## Startup Verification

- `GET /health` returned `Healthy`
- Sign-in page loaded over `HTTPS`
- Sign-in succeeded

## Surface Verification

- `/Requests/Workspace`
- `/Intake/Workspace`
- `/Operations/Queue`
- `/Approvals/Queue`
- `/Dispatch/Workspace`
- `/Administration/Users`

## Storage Verification

- Document staging succeeded
- File was written to the persistent documents root

## OCR Verification

- OCR enabled for this environment:
- OCR request executed:
- OCR result confirmed:

## Observability

- IIS logs reviewed
- Application stdout/stderr reviewed if enabled
- No startup guard failures observed
- No critical runtime errors observed

## Outcome

- Smoke passed:
- Go-live approved:
- Follow-up items:

## Notes

- 
