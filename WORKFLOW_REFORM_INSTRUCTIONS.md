# BG Workflow Reform Instructions

Status: Active
Last Updated: 2026-04-03
Audience: Product, UX, Engineering, QA, Operations Stakeholders

## 1) Purpose

This document captures the full institutional direction agreed in the discussion:

- Current problem statement
- Shared diagnosis model
- Shared solution levers
- Prioritized execution plan
- Current progress snapshot

It is intended to keep all contributors aligned on one operating model.

## 2) Core Direction

Target maturity is not "guided UI" with heavy explanatory text.

Target maturity is "deterministic operational UI" where:

- The correct step is visually and behaviorally the only logical next step
- Role, state, and policy constraints are encoded in the structure
- Explanatory text is minimized and never used as a substitute for flow design

## 3) Problem Statement (Current)

The system is institutionally structured and role-aware, but still shows recurring patterns that increase cognitive load:

- Parallel decisions with similar visual weight
- Screens whose visual order does not match real execution order
- Reliance on banners/alerts/message cards for guidance
- Queues shown as information lists rather than weighted work order
- Late correction model (let user attempt, then explain why blocked)

Result: experienced employees may still hesitate because the UI often explains after the fact instead of constraining before action.

## 4) Core Levers (Reduced and Non-Overlapping)

This model intentionally uses six core levers only.

Each lever has one diagnostic question to avoid overlap across teams.

### Lever 1: Visual Sequencing

Diagnostic question:

- Does screen order match real execution order?

Applies mainly to:

- src/BG.Web/Pages/Intake/_VerificationPanel.cshtml
- src/BG.Web/Pages/Requests/_RequestActivePanel.cshtml
- src/BG.Web/Pages/Dispatch/_DispatchActivePanel.cshtml
- src/BG.Web/Pages/Operations/_OperationsActivePanel.cshtml

### Lever 2: Reduce Parallelism

Diagnostic question:

- Does the user see more than one decision center in the same moment?

Applies mainly to:

- src/BG.Web/Pages/Approvals/_ApprovalActivePanel.cshtml
- src/BG.Web/Pages/Requests/_RequestActivePanel.cshtml
- src/BG.Web/Pages/Operations/_OperationsActivePanel.cshtml
- src/BG.Web/Pages/Dispatch/_DispatchActivePanel.cshtml

### Lever 3: Reduce Decision Density

Diagnostic question:

- Are there too many elements with similar visual weight?

Applies mainly to:

- src/BG.Web/Pages/Intake/_VerificationPanel.cshtml
- src/BG.Web/Pages/Operations/_OperationsActivePanel.cshtml
- src/BG.Web/Pages/Approvals/_ApprovalQueueList.cshtml
- src/BG.Web/Pages/Operations/_OperationsQueueList.cshtml

### Lever 4: Decision Weighting in Queues

Diagnostic question:

- Does the highest-priority item look naturally like the next item to process?

Applies mainly to:

- src/BG.Web/Pages/Approvals/_ApprovalQueueList.cshtml
- src/BG.Web/Pages/Operations/_OperationsQueueList.cshtml

### Lever 5: Silent Alternative Path

Diagnostic question:

- Does the page shape itself correctly before the user attempts an invalid action?

Applies mainly to:

- src/BG.Web/Pages/Approvals/Queue.cshtml.cs
- src/BG.Web/Pages/Approvals/Dossier.cshtml.cs
- src/BG.Web/Pages/Requests/Workspace.cshtml.cs
- src/BG.Web/Pages/Intake/Workspace.cshtml.cs

### Lever 6: Contextual Entry

Diagnostic question:

- Does the user start from role-aligned work context instead of a neutral landing?

Applies mainly to:

- src/BG.Web/Pages/Index.cshtml
- src/BG.Web/UI/WorkspaceShellService.cs

## 5) Cross-Cutting Principles (Not Separate Levers)

These principles must be applied with core levers and should not be tracked as standalone lever buckets.

### Principle A: Show Governance Early

- Expose policy constraints before interaction attempt.
- Prevent surprise rejection after user effort.

Primary surfaces:

- src/BG.Web/Pages/Approvals/_ApprovalActivePanel.cshtml
- src/BG.Web/Pages/Approvals/Queue.cshtml.cs
- src/BG.Web/Pages/Requests/Workspace.cshtml.cs
- src/BG.Web/Pages/Administration/Workflow.cshtml

### Principle B: Safe-by-Default

- Make safe behavior easier than risky behavior.
- Apply strongly on administration surfaces.

Primary surfaces:

- src/BG.Web/Pages/Administration/Users.cshtml
- src/BG.Web/Pages/Administration/Roles.cshtml
- src/BG.Web/Pages/Administration/Workflow.cshtml

### Principle C: Minimize Textual Guidance

- Move meaning from prose to structure and control availability.
- Keep text supportive, never flow-critical.

Primary surfaces:

- src/BG.Web/Pages/Intake/Workspace.cshtml
- src/BG.Web/Pages/Shared/_SurfaceMessageCard.cshtml
- src/BG.Web/Pages/Shared/_NotificationTray.cshtml

## 6) Priority Order (Core Levers)

1. Visual Sequencing
2. Reduce Parallelism
3. Reduce Decision Density
4. Decision Weighting in Queues
5. Silent Alternative Path
6. Contextual Entry

## 7) Execution Model

### Phase 1 (P0): Daily Operational Clarity

Focus:

- Visual Sequencing
- Reduce Parallelism
- Reduce Decision Density

Primary files:

- src/BG.Web/Pages/Intake/_VerificationPanel.cshtml
- src/BG.Web/Pages/Approvals/_ApprovalActivePanel.cshtml
- src/BG.Web/Pages/Requests/_RequestActivePanel.cshtml
- src/BG.Web/Pages/Operations/_OperationsActivePanel.cshtml
- src/BG.Web/Pages/Dispatch/_DispatchActivePanel.cshtml

### Phase 2 (P0/P1): Behavioral Path Enforcement

Focus:

- Silent Alternative Path
- Show Governance Early (principle)
- Minimize Textual Guidance (principle)

Primary files:

- src/BG.Web/Pages/Approvals/Queue.cshtml.cs
- src/BG.Web/Pages/Approvals/Dossier.cshtml.cs
- src/BG.Web/Pages/Requests/Workspace.cshtml.cs
- src/BG.Web/Pages/Intake/Workspace.cshtml.cs
- src/BG.Web/Pages/Shared/_SurfaceMessageCard.cshtml

### Phase 3 (P1): Queue and Entry Orchestration

Focus:

- Decision Weighting in Queues
- Contextual Entry

Primary files:

- src/BG.Web/Pages/Approvals/_ApprovalQueueList.cshtml
- src/BG.Web/Pages/Operations/_OperationsQueueList.cshtml
- src/BG.Web/UI/WorkspaceShellService.cs
- src/BG.Web/Pages/Index.cshtml

### Phase 4 (P2): Administrative Hardening

Focus:

- Safe-by-Default (principle)

Primary files:

- src/BG.Web/Pages/Administration/Users.cshtml
- src/BG.Web/Pages/Administration/Roles.cshtml
- src/BG.Web/Pages/Administration/Workflow.cshtml

## 8) Implementation Contracts (Required)

Use these contracts as mandatory constraints when implementing core levers.

### Contract 1: Visual Sequencing

Do:

- Match page order to real execution order.
- Place required inputs before optional context.

Do not:

- Require users to scan multiple sections before discovering first required step.

### Contract 2: Reduce Parallelism

Do:

- Keep exactly one primary action per active state.
- Move secondary actions behind an explicit advanced menu.

Do not:

- Render multiple same-weight CTA buttons for different outcomes.

### Contract 3: Reduce Decision Density

Do:

- Group fields and indicators by decision relevance.
- Hide low-relevance metadata behind disclosure controls.

Do not:

- Show all telemetry and fields at equal hierarchy.

### Contract 4: Decision Weighting in Queues

Do:

- Highlight top-priority item with clear visual dominance.
- Use stable ranking dimensions such as SLA, risk, due time, and policy urgency.

Do not:

- Present all queue entries with equal visual importance.

### Contract 5: Silent Alternative Path

Do:

- Shape the page at load time based on eligibility and state.
- Route users directly to valid path variants.

Do not:

- Let users submit first and then explain in error text why the action was invalid.

### Contract 6: Contextual Entry

Do:

- Route authenticated users to role-relevant workload entry by default.

Do not:

- Use neutral landing as default for operational roles.

## 9) Cross-Cutting Principle Contracts

### Principle Contract A: Show Governance Early

Do:

- Surface policy constraints before interaction.
- Make blocked paths visibly non-actionable by structure.

Do not:

- Reveal governance constraints only after user action fails.

### Principle Contract B: Safe-by-Default

Do:

- Make safe configuration easier than risky configuration.
- Require confirmation and summary diff for high-impact changes.

Do not:

- Allow ambiguous admin edits without guardrails.

### Principle Contract C: Minimize Textual Guidance

Do:

- Encode meaning through state, placement, and control availability.
- Keep explanatory text short and non-critical.

Do not:

- Depend on alert paragraphs to explain core flow.

## 10) Surface Definition of Done Checklists

Every changed surface must include checklist evidence in PR notes.

### Active Panels (Approvals, Requests, Operations, Dispatch)

- Single primary action is obvious in under 2 seconds.
- Action order matches real-world execution order.
- Non-eligible actions are hidden or structurally unavailable.
- Optional information does not compete with task-critical controls.

### Queue Surfaces

- Top item appears naturally actionable without extra sorting.
- Ranking dimensions are explicit and stable.
- Blocked items are visibly distinct before selection.

### Workspace Flow Surfaces

- Step progression is enforced by state, not by prose.
- Save/submit path is impossible unless prerequisites are met.
- Alternative path is rendered directly when user is ineligible.

### Administration Surfaces

- High-impact edits include confirmation and impact summary.
- Default action favors safe settings.
- Permissions/governance effects are visible before save.

## 11) Pull Request Template (Required)

Every PR touching scoped files must answer the following:

1. Lever(s) implemented:
2. Cognitive-load source removed:
3. Structural constraint added:
4. Text guidance removed or reduced:
5. Surface checklist items satisfied:
6. Expected measurable impact (which metric and direction):

## 12) Acceptance Criteria

A surface is considered aligned only when all are true:

1. Primary action is visually dominant and behaviorally appropriate to state/role.
2. Invalid actions are structurally unavailable (not just explained after click).
3. Reading burden is minimal; text does not carry core flow logic.
4. Queue naturally reveals processing priority.
5. Policy constraints are understood before action attempt.

## 13) Metrics (Operational)

Track weekly by role and surface:

- Time to first successful completion
- Completion rate without verbal help
- Number of interruption points per task
- Number of correction/error messages per completed task

Target indicators after two weeks of focused rollout:

- 80%+ completion without verbal coaching
- 60%+ reduction in "what should I do now" questions
- Measurable reduction in behavior-related operational errors

## 14) Team Working Rules

For any PR that touches listed surfaces:

1. PR must declare the lever(s) it implements.
2. PR must state which cognitive-load source was removed.
3. PR must include before/after rationale in behavioral terms, not only visual terms.
4. PR review should reject changes that add guidance text without adding structural constraint.
5. Non-draft PRs are CI-validated for template compliance via:
	- .github/workflows/pr-workflow-reform-gate.yml
	- scripts/ci/validate_pr_workflow_reform.py

## 15) Current State Snapshot (as of 2026-04-03)

- Role and permission enforcement foundations exist and are functional.
- Several surfaces are institutionally styled but still partly guidance-dependent.
- Team has agreed to move from explanation-heavy interaction to deterministic, role-constrained flow design.

---

If this document conflicts with local stylistic preference, follow this document for workflow behavior and contributor alignment.
