# Audit Remediation Backlog

## Purpose

This backlog converts the external audit reports into actionable remediation work.

Use this file to answer:

- which findings are accepted
- which findings are rejected or downgraded
- what must be fixed first
- what is sequenced after that

## Source Inputs

- [../../ui_ux_audit_report_ar.md](../../ui_ux_audit_report_ar.md)
- [../../full_runtime_audit_report.md](../../full_runtime_audit_report.md)
- [../../cognitive_audit_report.md](../../cognitive_audit_report.md)

## Audit Input Quality Notes

- The UI/UX audit is directionally strong for operational usability and is treated as a valid backlog input.
- `full_runtime_audit_report.md` and `cognitive_audit_report.md` are duplicate files with the same content hash and must be treated as one technical review, not two independent confirmations.
- Any claim in the technical audit that conflicts with the live codebase must be corrected here before it becomes backlog work.

## Accepted Findings

### Status Note

- `Priority 1` role-first shell/home behavior, `Approvals` action-first simplification, `Operations` evidence-first cleanup, `Intake` inline verification, and `Requests`/`Dispatch` minimal-mode rendering are now implemented in the live codebase.
- `Priority 2` authentication hardening, reverse-proxy trust configuration, permission cleanup, and shared parsing utility extraction are now implemented in the live codebase.
- `Priority 3` operational seed runtime boundary, OCR queue/file-size hardening, `TrackAggregateChildren()` caching, and `HospitalApi` placeholder removal are now implemented in the live codebase.
- Keep the accepted findings below limited to issues that remain open or require later cleanup.

### Security and Runtime

- Bootstrap seed password history is still embedded as a blocked password list in startup code.

### Domain and Architecture

- `OperationalSeedService` no longer runs from startup, but it still depends on application services from infrastructure and should remain treated as tooling rather than production runtime behavior.
- Historical request/evidence relinking still needs long-term review even after narrowing it to orphan evidence linked to the same correspondence.

## Rejected or Downgraded Findings

- The project is not `ASP.NET Core 9`; the live code targets `.NET 8`.
- The system does not lack integration-style tests entirely; hosted runtime tests exist. The real gap is production-like environment coverage.
- `dashboard.view` is not fully dead; it is used in home dashboard shaping, but it is not enforced through a standalone policy.
- `Privacy.cshtml` is not an empty placeholder anymore.
- The OCR review overstates `directory traversal` on `requestFilePath`; that path is generated internally. The real issue is runtime hardening, not traversal.
- `Swagger in production` is already guarded by production readiness validation. The remaining concern is configuration discipline, not missing protection.

## Execution Order

1. Remove operational clutter from the UI.
2. Fix role-to-surface behavior so users only see what they can act on.
3. Harden authentication and request-edge security.
4. Clean up permission model and dead authorization paths.
5. Correct domain/runtime integrity issues.
6. Refactor seed and OCR runtime boundaries.

## Priority 1

### 1. Role-First Shell and Home

- Redirect single-workspace users straight to their workspace.
- Hide sidebar when it adds no navigational value.
- Remove role-explainer cards, capability summaries, and instructional hero blocks from daily work pages.
- Show one empty-state card only when a role truly has no current work.

### 2. Approvals Action-First Rewrite

- Remove the inline checklist from [../../src/BG.Web/Pages/Approvals/_ApprovalActivePanel.cshtml](../../src/BG.Web/Pages/Approvals/_ApprovalActivePanel.cshtml).
- Remove repeated dossier summary content from the active panel.
- Keep only:
  - current item identity
  - evidence summary
  - decision controls
  - blocking reason when applicable
- Move timeline and deep dossier context behind explicit secondary navigation only.

### 3. Operations Evidence-Before-Action

- Move `System Matches` above the apply form in [../../src/BG.Web/Pages/Operations/_OperationsActivePanel.cshtml](../../src/BG.Web/Pages/Operations/_OperationsActivePanel.cshtml).
- Surface the top recommendation immediately.
- Keep the apply form unavailable until the evidence block is visible and selection is clear.
- Remove blocked-state filler cards that do not change the operator's next step.

### 4. Intake Inline Review

- Merge review metrics into the verification panel instead of keeping them in a detached summary card.
- Show review-required signals next to the actual fields being verified.
- Collapse non-critical counts and derived metrics unless the operator explicitly opens them.

### 5. Requests and Dispatch Minimal Mode

- Ensure empty `Requests` only shows request creation.
- Ensure empty `Dispatch` only shows one empty-state card and no side facts with no operational value.
- Remove any role-description blocks that explain what the role generally does.

### Acceptance Criteria

- Each daily role sees one clear next step.
- No page shows explanatory text about what the role can do unless explicitly opened.
- No empty workspace renders support rails, drawers, or repeated summary cards.

### Implementation Status

- Implemented

## Priority 2

### 6. Authentication Hardening

- Add login throttling or lockout behavior.
- Add audit logging for password changes and role assignment changes.
- Review controller CSRF posture and make state-changing controller actions explicitly protected or intentionally isolated.

### 7. Forwarded Headers and Edge Trust

- Replace open proxy trust with explicit known proxies or deployment-bound configuration.
- Keep production validation aligned with actual reverse-proxy topology.

### 8. Permission Model Cleanup

- Decide for each orphan permission:
  - implement a real protected surface
  - merge it into an existing permission
  - remove it from the catalog and seed roles
- Align shell navigation, policies, and role seeds to the same permission map.

### 9. Domain Integrity Fixes

- Prevent `CreateRequest()` on guarantees in terminal states.
- Review `ReopenAppliedBankConfirmation()` ordering guard and remove pure timestamp dependence where possible.
- Stop retroactive mutation of historical ledger associations, or document and isolate the exception clearly if it is a deliberate business rule.

### 10. Shared Parsing Utilities

- Extract amount/date normalization and parsing helpers into one shared application-level utility.
- Replace duplicated helper methods in intake, operations, and dispatch services.

### Acceptance Criteria

- Permission catalog, policies, and seeded roles are internally consistent.
- Login edge is rate-limited.
- Domain state transitions reject invalid actions earlier and more explicitly.

### Implementation Status

- Implemented

## Priority 3

### 11. Seed Runtime Boundary

- Move scenario/demo seeding out of application startup.
- Make operational demo data generation an explicit environment-only command or script.

### 12. OCR Runtime Hardening

- Add file-size guardrails before OCR execution.
- Move long-running OCR work away from request-thread blocking if production OCR remains enabled.
- Tighten worker lifecycle and timeout strategy further.

### 13. Technical Debt After Stabilization

- Revisit `BgDbContext.TrackAggregateChildren()` performance cost.
- Revisit `HospitalApi` placeholder integration and either complete or remove it.

### Implementation Status

- Implemented

## Priority Rule

- Execute the backlog strictly in order.
- Do not start a lower priority item while a higher priority item is still open, unless the lower item is required to unblock it.
- If a finding becomes invalid because the code changes, update this document before continuing.

## Out Of Scope For This Backlog

- broad visual redesign for branding alone
- SPA rewrite
- replacing Razor Pages
- speculative integrations without an approved business flow

## Definition Of Done

- The operator can enter a page and immediately identify:
  - what item needs attention
  - what evidence matters
  - what action is available now
- The page does not explain the role back to the user.
- Empty states are compact.
- Permission and policy model is internally consistent.
- Authentication and production edge behavior are hardened enough for controlled deployment.
