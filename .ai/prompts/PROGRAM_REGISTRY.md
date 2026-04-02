# BG AI Runtime Registry

This file is not a backlog.

It is a routing registry that tells external models where the **real** live
material for each workstream exists.

## Workstream Routing

### Axis A: Architecture & Boundaries
- Primary source:
  [../../docs/instructions/ARCHITECTURE.md](../../docs/instructions/ARCHITECTURE.md)
- Supporting plan:
  [../../docs/refactor_roadmap.md](../../docs/refactor_roadmap.md)

### Axis B: Operational Model
- Primary source:
  [../../docs/instructions/DAILY_OPERATIONS_MATRIX.md](../../docs/instructions/DAILY_OPERATIONS_MATRIX.md)
- Operator guides:
  [../../docs/instructions/ROLE_DAILY_CHECKLISTS.md](../../docs/instructions/ROLE_DAILY_CHECKLISTS.md)
  [../../docs/instructions/ROLE_SOPS_AR.md](../../docs/instructions/ROLE_SOPS_AR.md)

### Axis C: UI / UX
- Primary source:
  [../../docs/frontend_reconstruction_plan.md](../../docs/frontend_reconstruction_plan.md)
- Current remediation priority:
  [../../docs/instructions/AUDIT_REMEDIATION_BACKLOG.md](../../docs/instructions/AUDIT_REMEDIATION_BACKLOG.md)

### Axis D: Data & Domain Models
- Primary source:
  [../../docs/instructions/ARCHITECTURE.md](../../docs/instructions/ARCHITECTURE.md)
- Supporting plan:
  [../../docs/refactor_roadmap.md](../../docs/refactor_roadmap.md)

### Axis E: Strategic Closure
- Primary source:
  [../../docs/instructions/AUDIT_REMEDIATION_BACKLOG.md](../../docs/instructions/AUDIT_REMEDIATION_BACKLOG.md)
- Production closure:
  [../../docs/program_closure_backlog.md](../../docs/program_closure_backlog.md)

### Axis F: OCR Readiness
- Primary source:
  [../../docs/ocr_implementation_plan.md](../../docs/ocr_implementation_plan.md)
- Real sample baseline:
  [../../docs/reference/initial-bank-form-sample-catalog.md](../../docs/reference/initial-bank-form-sample-catalog.md)
- Regression assets:
  [../../tests/BG.UnitTests/TestData/Ocr/Wave1/README.md](../../tests/BG.UnitTests/TestData/Ocr/Wave1/README.md)

### Axis G: Production Readiness
- Primary source:
  [../../docs/instructions/PRODUCTION_DEPLOYMENT.md](../../docs/instructions/PRODUCTION_DEPLOYMENT.md)
- Execution procedure:
  [../../docs/instructions/PRODUCTION_RUNBOOK.md](../../docs/instructions/PRODUCTION_RUNBOOK.md)

## Runtime Rule

If an external model discovers something important:

- do not log it here as the primary project record
- write it back into the proper live document under `docs/`
- then use `.ai` only to improve the next external session
