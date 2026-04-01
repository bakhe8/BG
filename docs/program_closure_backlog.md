# Program Closure Backlog

## Document Role

- Status: `execution plan`
- Scope: consolidated mandatory closure backlog across all active plans
- Documentation index: [README.md](README.md)
- Architecture baseline: [../ARCHITECTURE.md](../ARCHITECTURE.md)
- Frontend execution plan: [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)
- OCR execution plan: [ocr_implementation_plan.md](ocr_implementation_plan.md)
- Production baseline: [../PRODUCTION_DEPLOYMENT.md](../PRODUCTION_DEPLOYMENT.md)
- Production execution procedure: [../PRODUCTION_RUNBOOK.md](../PRODUCTION_RUNBOOK.md)
- Repository testing policy: [../.github/instructions/testing.instructions.md](../.github/instructions/testing.instructions.md)
- Reference program plan: [audits/2026-03-12-master-program-execution-plan.md](audits/2026-03-12-master-program-execution-plan.md)

## Purpose

This file consolidates what is still open across the project's live plans.

Use it when the goal is no longer "what does each plan say?" but rather:

- what is still open overall
- what is mandatory to close
- what can be grouped into one execution backlog
- in what order the remaining work should close

This file does not replace the topic-specific source documents above. It
collects their still-open items into one closure program.

## Decision

The current program decision is:

- all still-open items across the active plans are required
- no remaining item is being treated as optional backlog
- closure means closing both:
  - first-production execution readiness
  - post-launch operational depth that is still marked open in the plans

## Closure Model

To avoid mixing launch work with later scale work, closure is split into three
mandatory milestones:

1. `Milestone A`: first-production closure
2. `Milestone B`: operational completion closure
3. `Milestone C`: scale and integration closure

The program is not considered fully closed until all three milestones are
complete.

## Milestone A: First-Production Closure

### Goal

Put `BG` into a real production environment safely and verify the baseline end
to end.

### Required Work

- prepare the production server and host baseline
  - `IIS`
  - `.NET 8 Hosting Bundle`
  - `PostgreSQL` connectivity
  - `TLS`
- configure production values
  - `ConnectionStrings:PostgreSql`
  - `Storage:DocumentsRoot`
  - `DataProtection:KeysPath`
  - `AllowedHosts`
- keep production-only guards correct
  - `OperationalSeed=false`
  - `Swagger=false`
  - `Ocr:Enabled` only when the local runtime is actually present
- publish and deploy the release package
- run migrations through the controlled deployment procedure
- execute the production smoke checklist
- confirm document storage writes to the persistent volume

### Closure Gate

`Milestone A` closes only when:

- the application starts under `Production`
- `/health` returns `Healthy`
- sign-in works
- `Requests`, `Intake`, `Operations`, `Approvals`, `Dispatch`, and
  `Administration` load over `HTTPS`
- document storage writes to the configured persistent path
- no development-only setting remains enabled

### Source Documents

- [../PRODUCTION_DEPLOYMENT.md](../PRODUCTION_DEPLOYMENT.md)
- [../PRODUCTION_RUNBOOK.md](../PRODUCTION_RUNBOOK.md)

## Milestone B: Operational Completion Closure

### Goal

Close the remaining work needed so the reconstructed system is not only live
but operationally settled.

### Track B1: Guided Operator Validation

Required work:

- run guided validation on the reconstructed surfaces
  - `Intake`
  - `Approvals`
  - `Requests`
  - `Operations`
  - `Dispatch`
  - `Administration`
- capture live friction by:
  - role
  - workflow state
  - blocked path
  - handoff point
- convert only proven friction into targeted refinements

Closure gate:

- each reconstructed surface has been exercised by real operators or realistic
  representatives
- remaining refinements are evidence-based, not speculative

Primary source:

- [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)

### Track B2: Targeted Frontend Refinements

Required work:

- apply role/state refinements proven by the guided validation pass
- add selective interaction enhancements only where the runtime gain is clear
- keep execution in the center, evidence in drawers/dossiers, and context in
  rails
- avoid broad shell or layout rewrites

Closure gate:

- no high-friction operator path remains unaddressed in the core surfaces
- no new frontend work violates the reconstruction boundary

Primary source:

- [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)

### Track B3: Exception and Recovery Completion

Required work:

- verify and close any still-open exception-first flows in live use, including:
  - returned request editing
  - request cancel/withdraw
  - operations reopen/correction
  - safe dispatch correction
  - explicit blocked and visibility explanations where required

Closure gate:

- common operator mistakes can be corrected through the system without manual
  database intervention

Primary source:

- [audits/2026-03-12-master-program-execution-plan.md](audits/2026-03-12-master-program-execution-plan.md)

### Track B4: Runtime Confidence Hardening

Required work:

- complete provider-safe verification across primary pages
- ensure hosted page-load coverage exists for primary route access
- keep mutation-failure observability actionable
- keep structured logging around critical state changes
- preserve regression coverage for provider/runtime failures

Closure gate:

- no known runtime confidence gap remains in the main operational path
- critical failures produce actionable logs
- primary routes remain covered by hosted smoke or deeper automated tests

Primary sources:

- [audits/2026-03-12-master-program-execution-plan.md](audits/2026-03-12-master-program-execution-plan.md)
- [../.github/instructions/testing.instructions.md](../.github/instructions/testing.instructions.md)

### Track B5: OCR Operational Completion

Required work:

- expand the bank-form library beyond the initial baseline
- add more bank profile entries
- add more structural document classes
- add per-bank expected field rules
- add per-bank reference patterns
- close runtime hardening for OCR:
  - timeout policy
  - structured logging
  - failure classification
  - retry policy where appropriate
  - hosted and integration tests
  - sample-based regression suite
  - page-scoped manifests for large bundles

Closure gate:

- scanned PDFs from the real sample baseline produce reviewable structured
  candidates
- text-native PDFs stay text-first
- route and provenance remain visible
- failures are logged and recoverable
- tests cover both text-first and OCR-first paths

Primary source:

- [ocr_implementation_plan.md](ocr_implementation_plan.md)

### Track B6: Throughput and Active Guidance

Required work:

- implement finish-and-next flows where justified
- strengthen next-action guidance
- improve exception-led queue progression
- reduce unnecessary return-to-queue friction across the core chain

Closure gate:

- highest-volume roles can move items forward with materially less friction
- next-action guidance is consistent across the core operational chain

Primary source:

- [audits/2026-03-12-master-program-execution-plan.md](audits/2026-03-12-master-program-execution-plan.md)

## Milestone C: Scale and Integration Closure

### Goal

Close the later-stage program items that were intentionally deferred until the
platform and core workbench were stable.

### Required Work

- expiring guarantee alerts
- queue aging and escalation
- reminders
- Oracle integration depth
- OCR/provider replacement depth beyond the first useful baseline
- external delivery automation
- richer output generation pipelines

### Closure Gate

`Milestone C` closes only when proactive management and deeper integrations can
be added without reopening foundation work.

### Primary Source

- [audits/2026-03-12-master-program-execution-plan.md](audits/2026-03-12-master-program-execution-plan.md)

## Program Execution Order

1. close `Milestone A`
2. close `Milestone B`
3. close `Milestone C`

Inside `Milestone B`, execute in this order:

1. guided operator validation
2. targeted frontend refinements
3. exception and recovery completion
4. runtime confidence hardening
5. OCR operational completion
6. throughput and active guidance

## What Is Already Closed

The following are not open closure items anymore:

- broad frontend reconstruction
- institutional shell stabilization baseline
- post-stabilization structural refactor for docs, UI organization, application
  organization, and hosted test organization

See:

- [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)
- [refactor_roadmap.md](refactor_roadmap.md)

## Definition Of Full Program Closure

The program is fully closed only when all of the following are true:

- `Milestone A` is complete
- `Milestone B` is complete
- `Milestone C` is complete
- the repository still satisfies:
  - `dotnet build BG.sln`
  - `dotnet test BG.sln`
- the hosted smoke subset remains green:
  - `dotnet test tests/BG.UnitTests/BG.UnitTests.csproj --no-build --filter "FullyQualifiedName~BG.UnitTests.Hosted.HostedSmokeTests"`

## Maintenance Rule

Whenever one of the closure tracks above is completed, update both:

1. this consolidated backlog
2. the topic-specific source document that owns the detailed topic
