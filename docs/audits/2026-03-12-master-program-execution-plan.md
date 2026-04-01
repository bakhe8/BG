# 2026-03-12 Master Program Execution Plan

## Purpose

This document consolidates the current implementation priorities into one
execution plan for completing BG with:

- correct operational behavior
- institutional alignment with KFSHRC
- stable runtime performance
- high operational confidence

It replaces ad-hoc sequencing. From this point forward, implementation should
follow this order unless a blocking defect forces a temporary deviation.

## 2026-03-13 Alignment Refresh

This plan must now be read together with:

- [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
- [README.md](../ui-proposals/README.md)
- [2026-03-13-component-role-visibility-matrix.md](2026-03-13-component-role-visibility-matrix.md)
- [ocr_implementation_plan.md](../ocr_implementation_plan.md)

The UI proposal library is now a binding execution input.

It changes the practical meaning of institutional alignment:

- institutional alignment is not only palette or typography
- it now also includes shell composure, role-based navigation, and component
  composition by task surface
- the proposal images should be treated as a component vocabulary and mandatory
  composition baseline, not as a set of isolated page mockups
- no shell or workspace reshape should be approved if it materially diverges
  from the proposal family without explicit pre-approval

## Current Baseline

The following foundations are already materially in place:

- trusted local authentication
- explicit authorization policies
- workflow integrity rules
- centralized canonical truth for core scenario and mapping logic
- ledger coverage across request, approval, operations, and dispatch
- hosted end-to-end test baseline
- paged queue/read-model foundation
- initial UX diagnostics and `antigravity` review
- proposal-library baseline in `docs/ui-proposals`
- component/role visibility matrix
- institutional shell and surface-zone contract
- reconstructed operational surfaces across:
  - `Intake`
  - `Approvals`
  - `Requests`
  - `Operations`
  - `Dispatch`
  - `Administration`
- first stable shared UI primitives
- first CSS modularization layer for shell and surface primitives

This means the next risk is no longer "missing the basic backbone". The next
risk is completing the program on top of surfaces, runtime habits, and
institutional assumptions that are still not fully locked.

## Non-Negotiable Rules

These rules apply to every remaining step.

1. No new feature branch is accepted without deciding:
   - which primary user owns it
   - which single page is the execution surface
   - which page, if any, is the deep dossier/reference surface

2. No new list or queue view is accepted without:
   - provider-safe query shape
   - pagination
   - mutation reads separated from listing reads

3. No state-changing operation is accepted without:
   - actor identity
   - ledger event
   - failure-safe behavior

4. No new operational page is accepted without:
   - hosted page-load test
   - authorization test
   - PostgreSQL-safe query behavior

5. No surface may mix all of the following by default:
   - execution
   - dossier depth
   - ledger depth
   - policy explanation
   - implementation notes

6. No design/system change may violate the institutional shell guardrails once
   Priority 0 is closed.

7. No new surface work is accepted unless it can be mapped to the component
   vocabulary implied by:
   - `docs/ui-proposals`
   - the component-role visibility matrix

8. No surface cleanup or shell refinement is accepted merely because it feels
   cleaner locally if it breaks proposal-library composition.

## Program Order

### Priority 0: Institutional Platform Guardrails

Goal:
- Lock the KFSHRC-compatible institutional shell before further surface
  reshaping.

Scope:
- freeze the default brand token set
- align the active green palette with institutional values
- define the supported typography stack
- constrain or remove conflicting theme variability
- standardize shell-level button, card, and navigation semantics

Why first:
- BG will be perceived as an internal hospital system, not a standalone tool
- rebuilding surfaces before locking the shell would create a second broad pass

Exit criteria:
- one approved default institutional theme
- no conflicting alternate themes exposed as equal first-class choices
- shell typography and action geometry fixed
- shared shell semantics documented and stable

### Priority 1: Core Decision Surface Realignment

Goal:
- Make the primary operational pages match the real decision model.

Order inside this priority:

#### 1A. Intake

Target:
- one focused verification surface
- deep processing context kept secondary

#### 1B. Approvals

Target:
- queue becomes scanning + selection + decision only
- dossier becomes the full evidence surface

#### 1C. Requests

Target:
- owner sees state, last outcome, and next action first
- ledger and deep tracking become secondary

#### 1D. Operations

Target:
- operator sees the item, best match, and confirmation path first
- provenance and support material stay secondary

#### 1E. Dispatch

Target:
- dispatch surfaces emphasize current handoff stage and next action
- evidence remains accessible but not dominant

Why this priority comes before more business logic:
- otherwise every new rule will be attached to the wrong interaction layer

Exit criteria:
- each primary page has one dominant decision purpose
- dossier/reference depth is not co-equal with execution by default
- each queue card is scan-friendly and materially lighter than its current form

### Priority 2: Exception-First Internal Logic

Goal:
- Close the internal business gaps that matter once the surfaces are correct.

Scope:
- edit returned requests
- cancel or withdraw requests
- reopen or correct operations decisions
- correct dispatch mistakes safely
- explicit "why not visible / why blocked" explanations where operationally
  required

Why here:
- these are the highest-value internal logic gaps still missing from the happy
  path backbone

Exit criteria:
- operators can recover from common mistakes without database intervention
- request lifecycle handles real institutional exceptions, not just happy paths

### Priority 3: Performance and Runtime Confidence Hardening

Goal:
- Make the system robust under real production behavior, not just functionally
  correct.

Scope:
- full PostgreSQL query audit for all primary pages
- provider-safe list/query patterns across the remaining screens
- page-load smoke coverage for all primary operational pages
- mutation-failure observability
- operational structured logging around critical state changes
- regression coverage for previous LINQ/provider failures

Why this is a distinct priority:
- performance and runtime confidence must be enforced before more automation
  and integration complexity arrives

Exit criteria:
- every primary page has provider-safe listing behavior
- no known PostgreSQL translation risks remain in the main operational flow
- hosted smoke coverage exists for primary route access and critical path pages
- critical failures produce actionable logs

### Priority 4: Operational Throughput and Active Guidance

Goal:
- Make the system operationally active instead of merely stateful.

Scope:
- finish-and-next flows where justified
- stronger next-step guidance
- exception-led queue progression
- better cross-handoff momentum

Why after Priority 3:
- throughput features on an unstable runtime base create noise and rework

Exit criteria:
- at least the highest-volume roles can complete items without unnecessary
  return-to-queue friction
- next-action guidance is consistent across the core operational chain

### Priority 5: Active Management, Alerts, and External Integration Depth

Goal:
- add scale features only after the platform is behaviorally and operationally
  stable.

Scope:
- expiring guarantee alerts
- queue aging and escalation
- reminders
- Oracle integration depth
- OCR/provider replacement depth
- external delivery automation
- richer output generation pipelines

Locked OCR direction inside this priority:

- `PyMuPDF` as the primary PDF text/raster boundary
- `PDFium` only as a compatibility fallback when PDF handling requires it
- `OpenCV` for preprocessing
- `LayoutParser` for layout detection
- `PaddleOCR` for recognition
- bank-form-aware post-processing into structured review fields

Important:

- `LayoutParser` is mandatory in the scanned-document path
- this priority should not implement a vague OCR provider contract; it should
  implement the above stack behind a replaceable BG integration boundary

Why last:
- these amplify the system; they should not be used to compensate for unclear
  surfaces or unstable runtime behavior

Exit criteria:
- the system can safely absorb proactive operations and external dependencies
  without reopening foundation work

## Delivery Method

Execution should proceed by priority, but each priority must be closed with the
same quality pattern:

1. define the owning user and decision purpose
2. adjust the page or runtime boundary
3. tighten tests
4. verify PostgreSQL behavior where relevant
5. verify ledger and authorization behavior where relevant
6. only then move to the next item

## What Is Explicitly Deferred

The following remain intentionally deferred until the earlier priorities close:

- ML-based matching
- advanced automation on confidence thresholds
- broad analytics expansion
- non-essential integrations
- ornamental visual experimentation outside the institutional shell

## Definition Of Safe Forward Progress

Work is considered to be moving at the correct pace only if all of the
following remain true:

- business logic is expanding on top of the correct decision surface
- the institutional shell is getting more stable, not more variable
- PostgreSQL/runtime confidence is increasing, not being assumed
- every new action remains attributable and auditable
- error recovery is improving, not being postponed

## Immediate Next Step

The current frontend reconstruction pass has crossed its safe boundary.

The next program step should now be:

**guided validation on the reconstructed workbench baseline, then continue
Priority 2 and selective frontend enhancement only where live friction is
verified**

Concretely:

1. treat the current shell + surface-zone contract as the locked baseline
2. validate the reconstructed surfaces against real role behavior on seeded
   operational data
3. use that validation to prioritize:
   - `Priority 2`: exception-first internal logic
   - selective interaction enhancements only where real queue or drawer
     friction is proven

Why this is next:

- the shell and zone model are no longer the main open risk
- the core workbench surfaces are now materially aligned
- broad additional frontend restructuring would create churn before live
  operator validation
