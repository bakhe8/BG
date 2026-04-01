# 2026-03-12 Execution Decision Backlog

## Purpose

This document turns the eight open strategic questions into one execution
backlog that will govern upcoming implementation decisions.

Its purpose is not to describe every future feature. Its purpose is to decide
what must be locked now, what must be introduced next, and what can safely wait
without creating structural rework.

## Decision Model

Items are classified into:

- `Must Have Now`: required before meaningful business expansion continues
- `Must Have Next`: should follow immediately after the current foundation is
  stable
- `Later / Selective`: useful, but should be introduced only when the concrete
  operating need is clear

## 2026-03-13 Alignment Refresh

This backlog must now be read together with:

- [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
- [README.md](../ui-proposals/README.md)
- [2026-03-13-component-role-visibility-matrix.md](2026-03-13-component-role-visibility-matrix.md)
- [ocr_implementation_plan.md](../ocr_implementation_plan.md)

New decision:

- `docs/ui-proposals` is no longer optional inspiration.
- It is now a binding component-direction source for future UI work.
- The images must be interpreted as a shared component vocabulary and mandatory
  composition baseline, not as a set of optional final screens.
- Any material deviation from the proposal-library structure requires explicit
  justification before implementation.

This changes the implementation emphasis inside the existing decisions:

- role separation now means `role + surface composition`, not only page access
- show/hide now means `role + workflow state + ownership + surface zone`
- Bootstrap remains the structural baseline, but the workbench composition model
  now comes from the proposal library and the visibility matrix

Current next-step implication:

- the institutional shell and shared surface-zone contract are now materially
  in place
- the active frontend move is no longer another broad shell/surface rewrite
- the next frontend step is guided operator validation on the reconstructed
  role/state surfaces, followed only by targeted refinements

## Executive Decision

The program should now be guided by three principles:

1. Build on real operating structure, not assumed structure.
2. Validate with realistic data before adding more business breadth.
3. Keep the technology base simple and institutionally aligned.

This means the most important inputs now are:

- real guarantee form shapes
- realistic seeded operational data
- role- and state-aware surface behavior
- focused restructuring of the execution surfaces

The least urgent item is broad library adoption.

## Item Decisions

### 1. Separate pages and interfaces by roles or users

Decision:
- `Must Have Now`

Why:
- BG is already organized around operational workspaces, not a universal
  one-size-fits-all console.
- The system will live inside a hospital internal systems landscape where users
  should remain inside their task environment.
- This reduces cognitive bleed between `Intake`, `Operations`, `Requests`,
  `Approvals`, `Dispatch`, and administration.

Implementation rule:
- Separate by `role + task surface`, not by hardcoded individual users.
- Individual user context matters for ownership and audit, but page ownership
  should still belong to the role-defined workflow.

Program effect:
- No future feature should be added without a named owning role and a primary
  execution surface.

### 2. Show and hide component sections per interface

Decision:
- `Must Have Now`

Why:
- Page-level separation alone is not enough.
- The same role may still need different depth depending on:
  - current workflow state
  - permission level
  - ownership
  - whether the user is executing or only reviewing
- Without this, screens become dossier-heavy and scale badly.

Implementation rule:
- Visibility logic must be driven by:
  - permission
  - workflow state
  - ownership
  - execution context
- UI hiding must never be the only enforcement layer; services remain the
  source of truth.

Program effect:
- This is a mandatory guardrail for every surface reshape from now on.
- This guardrail must be read together with the proposal library, not apart
  from it.

### 3. Use Bootstrap because it is used by the hospital official site

Decision:
- `Must Have Now` as the structural UI baseline

Why:
- The hospital's public platform already establishes Bootstrap-compatible
  composition behavior.
- BG already uses Bootstrap locally in the shell and page composition.
- Keeping Bootstrap as the shared layout base reduces institutional mismatch
  and lowers rework risk.

Important clarification:
- The decision is to keep `Bootstrap as the structural base`.
- The decision is not to ship raw default Bootstrap visuals.
- BG should remain a hospital-aligned custom layer over Bootstrap.

Current implication:
- Keep Bootstrap as the core layout/grid/component baseline.
- Avoid introducing a second competing UI framework.
- Any upgrade of the Bootstrap version should be controlled, not mixed with
  unrelated surface work.

Program effect:
- Institutional shell and component semantics remain stable while surfaces are
  improved.

### 4. Use external libraries to complete real operation

Decision:
- `Later / Selective`

Why:
- External libraries are necessary only for specific runtime gaps.
- Premature adoption creates integration surface area before the real workflow
  contracts are fully stable.

Allowed use cases later:
- PDF generation and print pipeline
- document signing/stamping
- email delivery
- OCR/provider integration
- structured observability if the native stack becomes insufficient

Not allowed as a broad default:
- adding general UI frameworks
- introducing utility libraries without a proven operational gap

Program effect:
- No new external library should be added unless it closes a specific verified
  production need.

### 5. Restructuring and reorganization

Decision:
- `Must Have Now`

Why:
- The program has passed the stage where simple feature accumulation is safe.
- We already have:
  - workflow governance
  - exception paths
  - ledger
  - provider-safe read models
  - institutional shell constraints
- Without ongoing reorganization, every new branch will amplify coupling.

Clarification:
- This is not a rewrite.
- This is controlled restructuring of boundaries, surfaces, contracts, and
  query shapes.

Program effect:
- Surface reshaping and internal exception handling must continue together, not
  as two separate late cleanup passes.

### 6. Real bank guarantee forms

Decision:
- `Must Have Now`

Why:
- This is one of the highest-priority missing realities in the system.
- Extraction, intake review, workflow routing, printing, and future automation
  should not keep depending on synthetic assumptions only.
- Sample fields are useful for scaffolding, but no longer sufficient for
  trustworthy business evolution.

Required outcome:
- A representative set of real form archetypes covering the core guarantee
  scenarios:
  - issuance
  - extension
  - release
  - reduction
  - status confirmation / bank reply patterns

Program effect:
- Intake and downstream logic should now be validated against real document
  structures, not only seeded scenario defaults.

2026-03-13 sample-baseline update:

- the first real scanned sample set has now been received and reviewed
- reference: [initial-bank-form-sample-catalog.md](../reference/initial-bank-form-sample-catalog.md)
- the reviewed files are image-based scans without a usable text layer
- this confirms `OCR provider` as the correct first external integration
- the first observed bank families are:
  - `Riyad Bank`
  - `BNP Paribas`
  - `ANB`
  - `BSF`
  - `SABB`
  - `SNB`
  - `Saudi Investment Bank`
- the first observed structural classes are:
  - `original-instrument`
  - `amendment-letter`
  - `extension-notice`

Canonical OCR pipeline decision:

- the production OCR path is now fixed as:
  - `PDF / Scan`
  - `PyMuPDF text-layer probe`
  - `PyMuPDF rasterization` with `PDFium` compatibility fallback only where needed
  - `OpenCV preprocessing`
  - `LayoutParser` layout detection
  - `PaddleOCR` recognition
  - `post-processing`
  - `structured field candidates`
  - `human review`
- `LayoutParser` is not optional in the scanned-document path
- direct PDF text extraction still has priority whenever a usable text layer exists
- OCR integration work should implement this stack rather than a generic OCR adapter with unspecified internals

### 7. Real users trying the system

Decision:
- `Must Have Next`, with early parallel involvement

Why:
- Real users are critical, but they become most useful when they can test:
  - realistic data
  - role-correct surfaces
  - real document shapes
- If they enter too early, feedback will be distorted by placeholder inputs.

What should happen now:
- Identify the real user groups immediately.
- Prepare them for structured trials.
- Start limited feedback loops as soon as the realistic data/form baseline is
  ready.

Program effect:
- This is the validation gate for the next wave of implementation, especially
  around `Intake`, `Approvals`, and `Requests`.

### 8. Seed generated data in the database, even if synthetic

Decision:
- `Must Have Now`

Why:
- The current infrastructure seed is foundational, not operational.
- Permissions, workflow templates, and a bootstrap admin are not enough to
  validate runtime behavior.
- We need realistic volume and state diversity to test:
  - queues
  - approvals
  - dispatch handoffs
  - correction paths
  - dashboard summaries
  - performance behavior

Required seed coverage:
- users across operational roles
- active and inactive workflows
- open and completed requests
- approval items in different stages
- dispatch items before and after delivery
- incoming bank responses
- correction and exception cases

Program effect:
- This becomes a prerequisite for reliable manual testing, UX validation, and
  PostgreSQL/runtime confidence.

## Final Priority Classification

### Must Have Now

- `1` Role/task-based page separation
- `2` Permission/state-aware component visibility
- `3` Bootstrap as the institutional structural base
- `5` Controlled restructuring and reorganization
- `6` Real bank guarantee forms
- `8` Realistic seeded database data

### Must Have Next

- `7` Real users trying the system in structured trials

### Later / Selective

- `4` External libraries only for proven operational gaps

## What This Changes In The Build Order

The next implementation decisions should now follow this sequence:

1. Strengthen the institutional surface contract already in place.
2. Build realistic seeded operational data packs.
3. Introduce real guarantee form archetypes into intake validation.
4. Continue surface reshaping with stronger role/state visibility logic.
5. Bring real users into guided trials on top of realistic data and forms.
6. Add external libraries only where the runtime need is now undeniable.

## Stop Rules

Do not continue broad business expansion if any of the following remain weak:

- no realistic seeded operational dataset
- no representative real guarantee forms
- unclear page ownership by role
- default screens still expose execution and dossier depth equally

Do not add a new external library merely because it may become useful later.

Do not treat real-user trials as a substitute for realistic seeded data.

## Immediate Program Direction

The most defensible next execution direction is:

1. build realistic seeded operational data
2. bring in real guarantee form archetypes
3. keep tightening role/state-aware decision surfaces

This sequence gives the program a better chance of moving forward without
another broad corrective pass later.

## 2026-03-13 Frontend Phase Closure Note

The current frontend reconstruction pass has now materially completed:

- institutional shell locking
- role/state surface-zone composition
- operational surface reconstruction
- first shared primitive extraction
- first CSS modularization layer

Therefore this backlog should no longer be read as if the UI still needs a
foundational shell reset before other work continues.

The next decision gate is:

- validate the reconstructed surfaces with realistic role usage
- then prioritize only the corrections that live usage proves necessary
