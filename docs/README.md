# Documentation Map

## Purpose

This index defines how documentation is organized in `BG` and which files are
considered authoritative for each topic.

Use this file when you need to know:

- which document to read first
- which document is the current source of truth
- which documents are execution plans
- which folders are reference or archive only

## Document Status Levels

- `source of truth`
  The current authoritative instruction set for an operational topic.
- `execution plan`
  A live implementation roadmap that must remain aligned with the codebase.
- `reference`
  Supporting background, examples, samples, or visual libraries.
- `archive`
  Historical analysis kept for context, not for direct execution.

## Read Order For Core Work

1. [ARCHITECTURE.md](../ARCHITECTURE.md)
2. [LOCAL_DEVELOPMENT.md](../LOCAL_DEVELOPMENT.md)
3. [testing.instructions.md](../.github/instructions/testing.instructions.md)
4. [PRODUCTION_DEPLOYMENT.md](../PRODUCTION_DEPLOYMENT.md)
5. [PRODUCTION_RUNBOOK.md](../PRODUCTION_RUNBOOK.md)

## Topic Ownership

- Architecture baseline and platform decisions:
  [ARCHITECTURE.md](../ARCHITECTURE.md)
- Local machine setup, local secrets, migrations, and OCR local environment:
  [LOCAL_DEVELOPMENT.md](../LOCAL_DEVELOPMENT.md)
- Repository-wide testing policy and verification gates:
  [testing.instructions.md](../.github/instructions/testing.instructions.md)
- Production configuration baseline and runtime guards:
  [PRODUCTION_DEPLOYMENT.md](../PRODUCTION_DEPLOYMENT.md)
- Production deployment execution procedure:
  [PRODUCTION_RUNBOOK.md](../PRODUCTION_RUNBOOK.md)
- Frontend evolution and reconstruction boundary:
  [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)
- OCR implementation order and locked technical decisions:
  [ocr_implementation_plan.md](ocr_implementation_plan.md)
- Post-stabilization refactor and documentation unification:
  [refactor_roadmap.md](refactor_roadmap.md)

## Current Source Documents

- [ARCHITECTURE.md](../ARCHITECTURE.md)
  Status: `source of truth`
  Scope: architecture baseline, platform decisions, quality floor, operational rules.
- [LOCAL_DEVELOPMENT.md](../LOCAL_DEVELOPMENT.md)
  Status: `source of truth`
  Scope: local prerequisites, secrets, migrations, OCR local setup.
- [testing.instructions.md](../.github/instructions/testing.instructions.md)
  Status: `source of truth`
  Scope: testing policy, definition of done, test priorities, and commands.
- [PRODUCTION_DEPLOYMENT.md](../PRODUCTION_DEPLOYMENT.md)
  Status: `source of truth`
  Scope: production configuration baseline and runtime guards.
- [PRODUCTION_RUNBOOK.md](../PRODUCTION_RUNBOOK.md)
  Status: `source of truth`
  Scope: executable production deployment procedure.

## Live Execution Plans

- [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)
  Status: `execution plan`
  Scope: frontend reconstruction direction and current completion boundary.
- [ocr_implementation_plan.md](ocr_implementation_plan.md)
  Status: `execution plan`
  Scope: OCR implementation order, locked decisions, and completion criteria.
- [refactor_roadmap.md](refactor_roadmap.md)
  Status: `execution plan`
  Scope: post-stabilization documentation, UI, application, and test refactor work.
- [program_closure_backlog.md](program_closure_backlog.md)
  Status: `execution plan`
  Scope: consolidated mandatory closure backlog across all active plans.

## Reference Material

- [ui-proposals/README.md](ui-proposals/README.md)
  Status: `reference`
  Scope: visual proposal library for shell, workspace, queue, drawer, and dossier patterns.
- [reference/initial-bank-form-sample-catalog.md](reference/initial-bank-form-sample-catalog.md)
  Status: `reference`
  Scope: early real sample baseline for bank forms and OCR direction.
- [audits](audits)
  Status: `reference`
  Scope: detailed diagnostic, execution, UX, and debt analysis snapshots.

## Archive Material

- [reference/antigravity-archive/README.md](reference/antigravity-archive/README.md)
  Status: `archive`
  Scope: historical UX and operational audit package retained for context.

## Maintenance Rules

- Any production behavior change must update the relevant source document in the same change.
- Any meaningful frontend direction change must update [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md).
- Any OCR pipeline change must update [ocr_implementation_plan.md](ocr_implementation_plan.md).
- Any program-level closure decision must update [program_closure_backlog.md](program_closure_backlog.md).
- If a document becomes historical only, mark it clearly as `archive` or `reference` instead of leaving it ambiguous.
- Avoid duplicating the same instruction across multiple files unless one file explicitly delegates to the other.

## Conflict Resolution

If two documents appear to conflict, resolve them in this order:

1. The most specific `source of truth` document for that topic wins.
2. A `source of truth` document overrides any `execution plan`, `reference`, or `archive` document.
3. An `execution plan` may refine how work should proceed, but it must not override the architectural or operational baseline.
4. `reference` and `archive` documents provide context only and must not be treated as live instructions when they conflict with current source documents.
