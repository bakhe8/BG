# 2026-03-13 Component and Role Visibility Matrix

## Purpose

This document converts the UI proposal images into a practical composition
matrix for BG.

The goal is not to describe final page designs. The goal is to define:

- what UI components exist in the visual direction
- which roles are allowed to see them
- where they should appear
- when they must stay hidden

This becomes the reference contract for future UI reconstruction so the system
does not drift back into "all information for all users on the same page."

## Decision Basis

This matrix is based on:

- [README.md](../ui-proposals/README.md)
- [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
- [2026-03-12-execution-decision-backlog.md](2026-03-12-execution-decision-backlog.md)
- [ARCHITECTURE.md](../instructions/ARCHITECTURE.md)
- [PermissionCatalog.cs](../../src/BG.Application/Security/PermissionCatalog.cs)
- [WorkspaceShellService.cs](../../src/BG.Web/UI/WorkspaceShellService.cs)

## Core Principle

The proposal images are not separate "finished screens."

They represent a shared component vocabulary that must be composed differently
per role, per workflow state, and per execution intent.

BG should therefore follow this rule:

`shared component system + role-based composition + state-based visibility`

## Binding Rule

The proposal library is now binding, not optional.

This matrix therefore does not merely interpret the proposal folder. It
operationalizes it.

The consequence is:

- no new shell or workspace composition may diverge materially from the
  proposal set unless there is a role- or state-based reason
- no "cleaner local version" may replace the proposal structure on personal
  judgment alone
- review of new UI work must include explicit comparison against the proposal
  family it belongs to

## 2026-03-13 Implementation Status

The matrix is no longer planning-only.

Its core composition model is now materially reflected in the implemented
frontend through:

- the institutional shell
- the canonical zones:
  - `Main`
  - `Support Rail`
  - `Detail Drawer`
  - `Dossier`
- reconstructed role-specific operational surfaces
- first shared primitives extracted from those surfaces

This means the matrix should now be used primarily as:

- a drift-prevention contract
- a validation tool for future UI changes
- a control surface for targeted refinements

It should not be treated as if the system is still waiting for its first
composition pass.

## Canonical Roles

For frontend composition, the system should now be treated as having these
canonical interface roles:

1. `Signed-in common user`
   - any authenticated internal BG user
2. `Intake operator`
   - document capture and verification
3. `Operations reviewer`
   - review lane, match suggestions, bank response application
4. `Request owner`
   - creates and tracks own requests only
5. `Approver / signer`
   - current decision surface for workflow stages
6. `Dispatch officer`
   - print, dispatch, delivery confirmation
7. `System administrator`
   - users, roles, delegations, workflow management
8. `Executive / dossier consumer`
   - not a separate permission model today, but a valid future read-heavy
     composition target for dossiers and leadership reviews

## Visibility States

Each component in this matrix uses one of these visibility modes:

- `Primary`
  - visible in the main execution surface
- `Secondary`
  - visible in a side rail, drawer, collapsed panel, or supporting region
- `On demand`
  - visible only after explicit open action, tab selection, or drill-in
- `Hidden`
  - should not be rendered for that role in the normal surface

## Placement Zones

All components should live in one of these zones:

- `Shell`
  - persistent navigation frame
- `Main`
  - the role's primary work area
- `Support rail`
  - secondary sidebar or compact context panel
- `Detail drawer`
  - contextual item details beside a list or queue
- `Dossier`
  - full read-heavy detail surface
- `Admin editor`
  - configuration or management form surface

These zones are mandatory implementation zones, not descriptive labels only.

## Component Families

The proposal set implies six stable component families:

1. `Shell components`
2. `Queue components`
3. `Document-centric workspace components`
4. `Decision components`
5. `Dossier components`
6. `Administration components`

---

## 1. Shell Components

| Component | Purpose | Placement | Common User | Intake | Operations | Requests | Approvals | Dispatch | Admin |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Left navigation shell | Primary module switching | Shell | Primary | Primary | Primary | Primary | Primary | Primary | Primary |
| Top bar identity block | signed-in identity and quick context | Shell | Primary | Primary | Primary | Primary | Primary | Primary | Primary |
| Global search | global command/search entry | Shell | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary |
| Notification cluster | alerts and incoming attention items | Shell | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary |
| Language switch | bilingual operation | Shell | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary |
| Quick settings / shell utilities | minor user-level utility actions | Shell | On demand | On demand | On demand | On demand | On demand | On demand | On demand |

### Shell Rules

- The shell must stay stable across all workspaces.
- Navigation is module-based, not page-link-heavy.
- A user should never need to scan top-level links for modules they cannot act
  on.
- Administrative links must stay isolated from operational links.

---

## 2. Queue Components

These components are inspired by the request, operations, and approval queue
proposals.

| Component | Purpose | Placement | Intake | Operations | Requests | Approvals | Dispatch | Admin |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Queue header | queue title, workload framing | Main | Hidden | Primary | Primary | Primary | Primary | Hidden |
| Queue tabs / lane switch | move between states or lanes | Main | Hidden | Primary | Primary | Secondary | Secondary | Hidden |
| Search bar | fast item location | Main | Secondary | Primary | Primary | Primary | Secondary | Primary |
| Filter row | status, department, due, lane filters | Main | Hidden | Primary | Primary | Secondary | Secondary | Secondary |
| Dense item list | high-scan operational list | Main | Hidden | Primary | Primary | Primary | Primary | Secondary |
| Active item panel | currently selected item summary | Main | Hidden | Primary | Primary | Primary | Primary | Hidden |
| Detail drawer | secondary item context | Detail drawer | Hidden | Primary | Primary | Secondary | Secondary | Hidden |
| Row action shortcuts | quick operational actions per item | Main | Hidden | Secondary | Secondary | Secondary | Secondary | Hidden |
| Paging footer | queue pagination | Main | Hidden | Primary | Primary | Primary | Primary | Primary |

### Queue Rules

- `Intake` is not queue-first. It is document-first.
- `Operations`, `Requests`, `Approvals`, and parts of `Dispatch` are queue-first.
- Detail drawers must not become a second full page inside the queue.
- Audit depth belongs in drawer or dossier, not in the row itself.

---

## 3. Document-Centric Workspace Components

These components come mainly from the split-view and intake-inspired images.

| Component | Purpose | Placement | Intake | Operations | Requests | Approvals | Dispatch | Executive |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Document viewer | scanned PDF/image review | Main | Primary | Secondary | On demand | On demand | On demand | Secondary |
| Document toolbar | zoom, page, print, search within document | Main | Primary | Secondary | On demand | On demand | On demand | Secondary |
| Verification form | field confirmation and structured save | Main | Primary | Hidden | Hidden | Hidden | Hidden | Hidden |
| Stepper / stage sections | sequential verification groups | Main | Primary | Hidden | Hidden | Hidden | Hidden | Hidden |
| Missing-field alerts | highlight unresolved extraction gaps | Main | Primary | Hidden | Hidden | Hidden | Hidden | Hidden |
| Detail preview side card | current entity summary beside document | Support rail | Primary | Secondary | Secondary | Secondary | Secondary | Secondary |
| Activity / notes tabs near document | local recent context | Support rail | Secondary | On demand | On demand | On demand | Hidden | Secondary |
| Extraction confidence chips | operator validation aid | Main | Secondary | Hidden | Hidden | Hidden | Hidden | Hidden |

### Document Workspace Rules

- The document viewer should only be primary where the user's job is document
  truth validation.
- For `Approvals` and `Requests`, the document is supporting evidence, not the
  main screen.
- `Operations` may surface the document in a secondary position when the bank
  response itself is the thing being resolved.

---

## 4. Decision Components

These components define the execution-heavy center of the system.

| Component | Purpose | Placement | Intake | Operations | Requests | Approvals | Dispatch | Executive |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Primary action bar | the main next action for the item | Main | Primary | Primary | Primary | Primary | Primary | Hidden |
| Decision buttons | approve, return, reject, verify, submit, dispatch | Main | Primary | Primary | Primary | Primary | Primary | Hidden |
| Comment / note field | action justification or contextual comment | Main | Secondary | Primary | Secondary | Primary | Secondary | Secondary |
| Governance warning block | explicit reasons why an action is blocked | Main | Hidden | Primary | Secondary | Primary | Secondary | Secondary |
| Next-step summary | what happens after action | Support rail | Secondary | Secondary | Primary | Primary | Primary | Secondary |
| Match suggestion panel | system proposal for linking the response | Main | Hidden | Primary | Hidden | Hidden | Hidden | Hidden |
| State summary card | current status and key values | Main | Secondary | Primary | Primary | Primary | Primary | Secondary |
| Ready / blocked badges | item readiness or blocker state | Main | Secondary | Primary | Primary | Primary | Primary | Secondary |

### Decision Rules

- The action area must always be closer to the item than the ledger.
- If a user cannot act, the surface should explain why in plain operational
  language.
- `Approvals` and `Operations` are decision-first.
- `Requests` is action-first only for the owner and only on their own items.

---

## 5. Dossier Components

These come from the executive dossier references and the dense approval
decision examples.

| Component | Purpose | Placement | Intake | Operations | Requests | Approvals | Dispatch | Executive |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Dossier header | identity, owner, stage, high-level metrics | Dossier | Hidden | On demand | On demand | Primary | On demand | Primary |
| Dossier left navigation | categorized read-heavy sections | Dossier | Hidden | Hidden | Hidden | On demand | Hidden | Primary |
| Summary tab | consolidated current truth | Dossier | Hidden | On demand | On demand | Primary | On demand | Primary |
| Documents tab | attachments and source documents | Dossier | Hidden | On demand | On demand | Primary | On demand | Primary |
| Timeline tab | chronological events and milestones | Dossier | Hidden | On demand | On demand | Primary | On demand | Primary |
| Audit log tab | full traceability trail | Dossier | Hidden | On demand | On demand | On demand | On demand | Primary |
| Notes tab | contextual notes and observations | Dossier | Secondary | Secondary | Secondary | Primary | Secondary | Primary |
| Attachments panel | supporting files | Secondary / Dossier | Secondary | Secondary | On demand | Primary | On demand | Primary |
| Previous decisions panel | prior approvals / returns / signatures | Secondary / Dossier | Hidden | Hidden | Hidden | Primary | Hidden | Primary |
| Alerts / notifications panel | pending risks and downstream notices | Secondary / Dossier | Secondary | Secondary | Secondary | Secondary | Secondary | Primary |

### Dossier Rules

- A dossier is not the default screen for operators.
- A dossier is the right place for read-heavy leadership and approval context.
- `Approvals` gets the strongest dossier relationship.
- `Requests`, `Operations`, and `Dispatch` should link into dossiers, not embed
  full dossiers inside the execution card.

---

## 6. Administration Components

| Component | Purpose | Placement | Intake | Operations | Requests | Approvals | Dispatch | Admin |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Admin grid | manage users, roles, delegations, workflows | Admin editor | Hidden | Hidden | Hidden | Hidden | Hidden | Primary |
| Entity filter bar | search and filter admin entities | Admin editor | Hidden | Hidden | Hidden | Hidden | Hidden | Primary |
| Side edit panel | edit selected entity without leaving context | Admin editor | Hidden | Hidden | Hidden | Hidden | Hidden | Primary |
| Workflow stage editor | configure stages and delegation policy | Admin editor | Hidden | Hidden | Hidden | Hidden | Hidden | Primary |
| Permission badges | summarize access attached to a role | Admin editor | Hidden | Hidden | Hidden | Hidden | Hidden | Primary |
| Admin audit section | config change history | Admin editor | Hidden | Hidden | Hidden | Hidden | Hidden | Secondary |

### Administration Rules

- Admin surfaces are configuration-first, not dossier-first.
- Admin surfaces must not reuse operational decision layouts.
- Administrative detail should not leak into day-to-day operational workspaces.

---

## Role Composition Matrix

This section translates components into actual role-specific surfaces.

### A. Intake Operator

Primary surface:
- document viewer
- document toolbar
- verification form
- grouped step sections
- missing-field and confidence guidance
- save/submit action bar

Secondary only:
- entity preview
- local notes
- recent activity

Hidden by default:
- approval history
- dispatch controls
- admin controls
- full dossier navigation

### B. Operations Reviewer

Primary surface:
- review queue
- selected item decision panel
- bank response match suggestions
- action confirmation form
- blocker / governance explanation

Secondary only:
- source document preview
- bank form alignment evidence
- recent activity / notes

On demand:
- deeper dossier
- full audit trail

Hidden by default:
- request creation form
- approval signer panels
- administrative editors

### C. Request Owner

Primary surface:
- own requests queue
- selected request status summary
- proposed change summary
- create / submit / withdraw / revise actions

Secondary only:
- next-step guidance
- lightweight timeline hints
- supporting drawer

On demand:
- dossier
- attachments
- detailed ledger

Hidden by default:
- other users' requests
- approval governance internals beyond what affects this owner
- dispatch execution controls

### D. Approver / Signer

Primary surface:
- approvals queue
- current decision panel
- decision buttons
- current stage explanation
- previous decisions
- attachments relevant to approval

Secondary only:
- governance blockers
- requester/request overview
- short next-step consequences

On demand:
- full dossier
- full audit log
- document-centric review

Hidden by default:
- request creation
- operational matching logic
- dispatch handoff controls
- administration entities

### E. Dispatch Officer

Primary surface:
- ready-to-dispatch queue
- pending-delivery queue
- print / dispatch / confirm-delivery action panel

Secondary only:
- dispatch evidence
- source form/bank summary
- handoff notes

On demand:
- request dossier
- outbound evidence details

Hidden by default:
- approval action controls
- operational match suggestions
- verification forms

### F. System Administrator

Primary surface:
- users grid
- roles grid
- delegations grid
- workflow editor

Secondary only:
- permission summaries
- config impact notes
- change history

On demand:
- operational example dossiers only when needed for support

Hidden by default:
- live execution controls inside intake, approvals, operations, or dispatch

### G. Executive / Dossier Consumer

Primary surface:
- dossier header
- summary metrics
- documents
- timeline
- audit trail
- alerts

Secondary only:
- notes
- high-level attachments

Hidden by default:
- low-level execution forms
- queue triage controls
- operator-only validation UI

---

## Component Ownership Rules

Each component family must have a primary owning surface.

| Family | Owning Surface |
| :--- | :--- |
| Document viewer + verification | Intake |
| Dense triage queues | Operations, Requests, Approvals, Dispatch |
| Decision bar / execution form | Operations, Requests, Approvals, Dispatch |
| Full dossier | Approvals first, then shared as read-heavy drill-in |
| Admin grid + editor | Administration |

This means:

- if a component is owned by `Intake`, another surface may reference it but
  should not clone its full depth inline
- if a component is owned by `Approvals` dossier, other pages should link to
  it rather than embed a full copy

## Mandatory Stop Rules

Do not implement a future surface if any of these violations are present:

- the page shows both execution UI and full dossier depth at the same level
- the role sees components that do not map to its next real action
- the same component appears in full form across multiple surfaces without a
  declared owner
- a queue row is carrying timeline, ledger, notes, and action controls all at
  once
- a role has to read administrative or governance internals to complete a
  normal task

## Immediate Use In Development

Before any upcoming UI implementation, each screen change should now answer
these questions:

1. What is the role?
2. What is the primary action?
3. Which component family owns that action?
4. Which supporting components are `Secondary` only?
5. Which components must stay hidden for this role?
6. Should detail live in the main surface, a drawer, or a dossier?

## Final Direction

The UI proposal folder should now be treated as a component source library and
a mandatory composition baseline, not as a set of standalone page mockups.

The operational direction for BG is therefore:

- stable institutional shell
- role-specific workbench composition
- queue or document first depending on the role
- full dossiers only where read-heavy decision depth is actually needed
- strong separation between execution, support context, and audit depth
