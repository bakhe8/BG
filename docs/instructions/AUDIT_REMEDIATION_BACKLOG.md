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

### Acceptance Criteria 1

- Each daily role sees one clear next step.
- No page shows explanatory text about what the role can do unless explicitly opened.
- No empty workspace renders support rails, drawers, or repeated summary cards.

### Implementation Status 1

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

### Acceptance Criteria 2

- Permission catalog, policies, and seeded roles are internally consistent.
- Login edge is rate-limited.
- Domain state transitions reject invalid actions earlier and more explicitly.

### Implementation Status 2

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

- Implemented

## Reviewed Wave 1 UI Findings

### 14. [C-SURF-01] Landing urgency visibility

- **Status:** Implemented for the accepted scope.
- **Current reality before fix:** The home page was role-scoped but still presented count cards and workspace options with equal visual weight, which diluted the first action for the signed-in actor.
- **Evidence:** [../../src/BG.Web/Pages/Index.cshtml](../../src/BG.Web/Pages/Index.cshtml), [../../tests/BG.UnitTests/Hosted/HostedAuthenticationAndHomeTests.cs](../../tests/BG.UnitTests/Hosted/HostedAuthenticationAndHomeTests.cs)
- **Adoption note:** The accepted remediation was to make the landing surface explicitly start with one primary workspace card and demote secondary workspaces, without introducing a new urgency engine or queue-specific ranking model.
- **Resolution:** The home page now renders a primary `Start here` work card first and relegates any remaining workspaces to a secondary section instead of treating all cards as equal.
- **Target:** Highlight explicit urgent items only where queue pressure justifies them, while preserving a single clear starting action on the landing page.
- **Priority:** Medium

### 15. [C-SURF-02] Owner execution clarity in Requests

- **Status:** Implemented for the accepted scope.
- **Current reality before fix:** The revise/resubmit path was buried inside collapsible sections, which slowed the owner when the request came back for changes.
- **Evidence:** [../../src/BG.Web/Pages/Requests/_RequestActivePanel.cshtml](../../src/BG.Web/Pages/Requests/_RequestActivePanel.cshtml), [../../tests/BG.UnitTests/Hosted/HostedRequestsAndIntakeTests.cs](../../tests/BG.UnitTests/Hosted/HostedRequestsAndIntakeTests.cs)
- **Adoption note:** The accepted remediation was to surface the editable request form directly in the active panel. Broader layout removal proposals remain out of scope until a separate review justifies them.
- **Resolution:** Returned/editable requests now render a visible revision card with update controls in the active panel, while secondary reference sections remain collapsed.
- **Target:** Keep the active owner action visible when the request is editable, while secondary reference stays collapsed.
- **Priority:** High

### 16. [C-SURF-03] Approval decision split between dossier and queue

- **Status:** Implemented.
- **Current reality before fix:** Approvers read the full file in the dossier, then had to navigate back to the queue to approve, return, or reject.
- **Evidence:** [../../src/BG.Web/Pages/Approvals/Dossier.cshtml](../../src/BG.Web/Pages/Approvals/Dossier.cshtml), [../../src/BG.Web/Pages/Approvals/Dossier.cshtml.cs](../../src/BG.Web/Pages/Approvals/Dossier.cshtml.cs), [../../tests/BG.UnitTests/Hosted/HostedRequestsAndIntakeTests.cs](../../tests/BG.UnitTests/Hosted/HostedRequestsAndIntakeTests.cs)
- **Resolution:** The dossier now contains the active decision surface and can execute `Approve`, `Return`, and `Reject` directly from the full-file view. Successful actions still return to the queue after execution so the approver lands back on the operational list.
- **Target:** Keep the decision bar on the same surface where the approver reads the full request evidence.
- **Priority:** Critical

### 17. [C-SURF-04] Approvals Missing Inline Attachment Preview

- **Status:** Implemented.
- **Current reality before fix:** Approvers cannot view supporting document evidence without downloading or navigating away from the decision surface.
- **Evidence:** [../../src/BG.Web/Pages/Approvals/Dossier.cshtml](../../src/BG.Web/Pages/Approvals/Dossier.cshtml) (Lines 403-445)
- **Resolution:** Integrated a Detail Drawer and preview handler that streams document content directly into the Dossier view.
- **Target:** Keep the decision bar on the same surface where the approver reads supporting evidence.
- **Priority:** Critical

### 18. [C-SURF-05] Intake document-first split is still incomplete

- **Status:** Implemented.
- **Current reality before fix:** `Intake` promised a document-first workbench, but the live page showed upload metadata only and no embedded source-document viewer.
- **Evidence:** [../../src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml](../../src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml), [../../src/BG.Web/Pages/Intake/Workspace.cshtml.cs](../../src/BG.Web/Pages/Intake/Workspace.cshtml.cs), [../../tests/BG.UnitTests/Hosted/HostedRequestsAndIntakeTests.cs](../../tests/BG.UnitTests/Hosted/HostedRequestsAndIntakeTests.cs), [../frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
- **Resolution:** `Intake` now serves the staged source document through a preview handler and renders the live PDF/image beside the verification form, with a direct open-document action for full inspection.
- **Target:** Keep the staged source document visible beside the verification form so the operator can review extracted fields against the actual file.
- **Priority:** Critical

### 19. [C-SURF-06] Persistent Intake Capture Zone Vertical Overhead

- **Status:** Implemented.
- **Current reality before fix:** The file upload field remains prominent even after a draft exists, pushing critical document evidence below the fold.
- **Evidence:** [../../src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml](../../src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml)
- **Resolution:** The Capture Zone is now automatically collapsed into a `<details>` element when a draft document is active.
- **Target:** Maximize vertical space for document evidence during verification.
- **Priority:** High

### 20. [C-SURF-07] Static Document Preview without Operational Controls

- **Status:** Implemented (Integrated with [C-SURF-02]).
- **Current reality before fix:** Document evidence in `Intake` was rendered without Rotate/Zoom controls.
- **Evidence:** [../../src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml](../../src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml) (Lines 69-81)
- **Resolution:** Integrated Rotate and Zoom controls into the preview viewport with immediate CSS-driven updates.
- **Target:** Ensure evidence legibility without breaking the verification context.
- **Priority:** Medium

### 21. [C-SURF-09] Single-Stack Dossier vs. Multi-Zone Workbench

- **Status:** Implemented.
- **Current reality before fix:** The `Approvals/Dossier` surface used a single vertical stack.
- **Evidence:** [../../src/BG.Web/Pages/Approvals/Dossier.cshtml](../../src/BG.Web/Pages/Approvals/Dossier.cshtml)
- **Resolution:** Restructured the Dossier into a dual-pane layout with a sticky Support Rail for Timeline and Signatures.
- **Target:** Align the Dossier with the institutional shell master pattern.
- **Priority:** High

## Reviewed Wave 2 Architecture Findings

### 22. [A-ARCH-01] Operational seeding crosses layers in a way that is convenient but architecturally impure

- **Status:** Approved for Remediation
- **Current reality:** `OperationalSeedService` in `Infrastructure` orchestrates `Application` services, creating an architectural impurity.
- **Evidence:** `src/BG.Infrastructure/Persistence/OperationalSeedService.cs`, `src/BG.Infrastructure/Persistence/DatabaseInitializationExtensions.cs`
- **Target state:** Extract operational scenario composition to a dedicated seed tooling module consuming `Application` contracts.
- **Priority:** Medium | **Cost:** Medium | **Risk:** Low

### 23. [A-ARCH-02] Web host owns excessive cross-cutting composition concerns

- **Status:** Implemented.
- **Current reality before fix:** `Program.cs` in `Web` handled registrations for auth, localization, security, and readiness, growing into a complex composition root.
- **Evidence:** `src/BG.Web/Program.cs`
- **Resolution:** Modularized composition into extension modules (`AddBgSecurity()`, `AddBgProjectServices()`, `AddBgWebDefaults()`) and middleware orchestration (`UseBgWebDefaults()`, `UseBgSecurity()`).
- **Target state:** Modularize composition into extension modules (e.g., `AddBgAuth()`, `AddBgLocalization()`).
- **Priority:** Medium

### 24. [A-ARCH-03] Architecture profile exposure is static metadata, not a governed contract

- **Status:** Deferred/Informational
- **Current reality:** `IArchitectureProfileService` provides descriptive metadata but lacks drift detection or scorecard/validation logic.
- **Evidence:** `src/BG.Application/Services/Platform/ArchitectureProfileService.cs`, `src/BG.Web/Controllers/SystemController.cs`
- **Target state:** Add governance scorecard logic or validated capability flags if official architectural verification is required.
- **Priority:** Later | **Cost:** Low | **Risk:** Low

## Reviewed Wave 2 Implementation & Readiness Findings

### 25. [G-PROD-01] Missing Production Configuration Baseline

- **Status:** Implemented.
- **Current reality before fix:** The project lacked a dedicated `appsettings.Production.json` file.
- **Evidence:** `src/BG.Web/appsettings.json`, `src/BG.Web/Configuration/ProductionReadinessValidator.cs`
- **Resolution:** Created `appsettings.Production.json` with production-hardened defaults for OCR and storage.
- **Target state:** Create and enforce an `appsettings.Production.json` baseline for environment-aware configurations.
- **Priority:** Critical

### 26. [G-PROD-02] Shared Data Protection Keys for IIS Clusters

- **Status:** Implemented.
- **Current reality before fix:** Shared storage/distribution for multi-node IIS was missing.
- **Evidence:** `src/BG.Web/Program.cs` (Lines 23-36)
- **Resolution:** Configured `DataProtection` in `Program.cs` to persist keys to a configurable directory with guidance for UNC/SQL migration.
- **Target state:** Move DataProtection keys to a shared UNC path or distributed provider (Redis/SQL).
- **Priority:** High

### 27. [D-IMPL-01] Global Exception and Logging Sanitization

- **Status:** Implemented.
- **Current reality before fix:** Specialized telemetry sanitization for production logs was missing.
- **Evidence:** `src/BG.Web/Program.cs` (Line 126)
- **Resolution:** Added a custom `UseExceptionHandler` lambda in `Program.cs` that logs sanitized errors with Trace IDs and redirects to a generic error page.
- **Target state:** Add production-grade exception sanitization to correlate and strip sensitive data from logs.
- **Priority:** Medium

> [!NOTE]
> Based on the Axis A Review (Session: 20260402-163739-wave-wave2-axis-a), the current stack (ASP.NET Core 8, Razor Pages, PostgreSQL) and five-project modular boundary are to be preserved as the enterprise-capable baseline. No rewrite to SPA or collapse of the integration layer is required.

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
