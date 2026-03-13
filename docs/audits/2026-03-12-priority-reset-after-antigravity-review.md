# Priority Reset After `antigravity` Review

## Purpose

This document resets implementation priorities after a full review of the
`antigravity` folder. The goal is to prevent continued feature growth on top of
interaction surfaces that are structurally misaligned with the intended
operational model.

## 2026-03-13 Status Refresh

This document should now be treated as the **first reset point**, not the final
active reference for frontend direction.

The `antigravity` review has now been superseded on the UI side by:

- [README.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/ui-proposals/README.md)
- [frontend_reconstruction_plan.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/frontend_reconstruction_plan.md)
- [2026-03-13-component-role-visibility-matrix.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/audits/2026-03-13-component-role-visibility-matrix.md)

Updated interpretation:

- `antigravity` established that the old surfaces were structurally misaligned
- `ui-proposals` now defines the richer component vocabulary and layout
  composition direction that should guide implementation

## What The `antigravity` Review Changed

The `antigravity` material is not a visual refresh package. It defines an
operational product model:

- BG is a guarantee-centered operational governance platform.
- The UI should follow decision sequence, not database structure.
- Lists should optimize scanning and prioritization, not carry full dossier
  weight.
- Deep evidence, history, provenance, and policy context should appear on
  demand, not by default.
- The system should support management by exception and guide the next action.

This means the current highest risk is no longer "missing another business
rule". The highest risk is continuing to add business behavior on top of
surfaces that still mix execution, explanation, ledger, and dossier context in
the same interaction layer.

## Current Structural Mismatch

### 1. Intake remains structurally overloaded

Current implementation still combines:

- hero state
- scenario selection strip
- actor context
- three-pane data entry
- review table
- save outcome summary
- processing context
- pipeline explanation
- future integration notes

This is materially different from the focused intake review model in
`antigravity`, where the operator should mainly verify one document against one
decision surface.

### 2. Approvals are improved but not yet true master-detail

Current implementation has a separate dossier page, but the queue still carries:

- request summary
- why-here explanation
- what-to-confirm explanation
- next-step explanation
- governance blocking context
- dossier summary counters
- inline action form

This keeps the queue heavier than the target operational model.

### 3. Requests still over-expose tracking detail

The request owner surface still shows:

- current state summary
- last decision
- tracking summary
- ledger counts
- expandable ledger with approval, dispatch, and operations policy metadata

This is better than before, but still closer to a dossier surface than a
request-owner action surface.

### 4. Operations and Dispatch still mix execution with reference material

Both queues still expose large support sections inside list cards:

- why-here
- evidence/provenance
- next-step explanation
- forms
- matching detail or print/delivery detail

This will scale poorly as volume grows.

## Priority Reset

## Stop Rule

Do not continue with new business capability expansion until the core decision
surfaces are aligned with the operational model below.

Specifically pause new work on:

- new request states
- new automation rules
- reminder/escalation engines
- new integration breadth
- additional policy branches

unless required to unblock one of the priority items below.

## New Execution Order

### Priority 0: Institutional Platform Guardrails

This now sits ahead of the surface reset because BG will be deployed as part of
the KFSHRC internal systems landscape.

The institutional alignment work is not a cosmetic theme pass. It is a
platform-level constraint that should be locked before deeper surface work
continues.

Scope:

- freeze the institutional token set
- align the default accent system to KFSHRC brand values
- define the approved typography strategy
- establish the supported institutional shell direction
- remove or constrain theme variability that conflicts with a hospital-native
  product identity
- keep Bootstrap-based composition as the structural base while the hospital
  identity is layered on top consistently

Why this comes first:

- the application will be judged as part of the hospital software estate
- if decision surfaces are rebuilt before the institutional shell is fixed,
  they may need another broad pass for typography, tokens, spacing, and control
  semantics
- the current implementation already has a compatible custom CSS token layer,
  so this is relatively low-risk and high-leverage

Important:

Priority 0 does not replace the surface reset. It defines the design-system
guardrails that all later surface work must obey.

### Priority 1: Decision-Surface Realignment

This is now the top priority.

#### 1A. Intake

Goal:
- Turn intake into a focused verification surface.

Why first:
- Highest operator frequency
- Highest concentration of avoidable cognitive load
- Current page shape is furthest from the target model

#### 1B. Approvals

Goal:
- Make the queue a true scanning-and-selection surface.
- Keep dossier depth in the dossier page, not in the queue card.

Why second:
- Approval is the highest-governance decision surface.
- Complexity here damages trust and decision speed.

#### 1C. Requests

Goal:
- Make the owner view primarily about request state, next action, and outcome.
- Keep deep timeline/ledger context secondary.

Why third:
- The current request surface still inherits dossier behavior.

### Priority 2: Exception-First Operational Flow

After the surfaces above are corrected, implement the business logic that fits
the operational model:

- edit returned requests
- cancel/withdraw request paths
- reopen/correct operations decisions
- correction paths for dispatch mistakes
- explicit "why not in my queue" explanations where needed

Reason:
- These are the internal logic gaps that matter most after the surface
  structure is corrected.

### Priority 3: Conveyor and Next-Action Logic

After exception paths exist, make the system more operationally active:

- finish-and-next flow where appropriate
- stronger next-action prompts
- better queue progression behavior
- exception-led navigation instead of passive status browsing

Reason:
- `antigravity` repeatedly points toward throughput and decision momentum, not
  static workspace browsing.

### Priority 4: Active Operational Management

Only after the above:

- expiring guarantee alerts
- aging queues
- reminders
- escalation rules

Reason:
- Alerts on top of the wrong surface architecture would amplify noise, not
  clarity.

### Priority 5: External Integration Depth

Only after the system behaves correctly on its own decision surfaces:

- production OCR/provider replacement
- Oracle integration depth
- external document delivery automation
- richer output generation pipelines

## What This Means In Practice

The next implementation track should not start from another business feature.

It should start from these concrete surface corrections:

1. Reduce intake to a task-first verification surface.
2. Strip approvals queue back to selection + decision essentials.
3. Keep dossier depth in dossier.
4. Reduce requests cards to state, outcome, and next action.
5. Push non-essential support context behind secondary disclosure in operations
   and dispatch.

## Why This Order Is Safer

This sequence reduces the chance of future rework because it aligns:

- domain behavior
- page responsibility
- decision depth
- exception handling
- future automation

If we continue adding logic before this reset, later corrections will require
touching both behavior and presentation repeatedly.

## Final Decision

This document's original final decision should now be treated as a **historical
reset point**.

The newer and active interpretation is:

- `antigravity` exposed the structural mismatch
- `ui-proposals` now provides the richer component and composition direction
- the next step is no longer "surface simplification only"
- the next step is:
  - lock the institutional shell
  - lock the shared surface zones
  - then continue surface work on that composition baseline
