# BG Daily Operations Matrix

## Document Role

- Status: `source of truth`
- Scope: implemented daily operating model, role-step-decision matrix, and live validation boundary
- Documentation index: [README.md](../README.md)
- Related documents:
  - [ARCHITECTURE.md](ARCHITECTURE.md)
  - [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
  - [ocr_implementation_plan.md](../ocr_implementation_plan.md)
  - [program_closure_backlog.md](../program_closure_backlog.md)

## Boundary

This file explains how `BG` is expected to be used day to day based on the
live codebase.

Use this file when you need to know:

- which role does what each day
- which decision belongs in which workspace
- which state transitions are allowed
- where the current implementation is strong
- where live operator validation is still required

Use [ARCHITECTURE.md](ARCHITECTURE.md) for higher-level policy and platform
rules.

Use this file for the implemented operating flow.

Important boundary:

- this file describes the current implemented model
- it does not claim to fully capture every unwritten field shortcut or local
  habit used by operators in the real hospital environment

## Core Daily Model

`BG` is a document-driven operating system for bank guarantees.

The daily logic currently implemented is:

1. `Intake` registers or updates the documentary truth.
2. `Requests` creates the outgoing business action against a guarantee.
3. `Approvals` moves the request through the configured signing chain.
4. `Dispatch` prints, records, and confirms delivery of the approved letter.
5. `Operations` applies the incoming bank response back onto the guarantee and
   closes the external loop.

This means the system is not centered on free-form case notes.

It is centered on:

- guarantee records
- request lifecycle state
- official correspondence
- signed outgoing letters
- scanned incoming bank responses

## Cross-Cutting Daily Rules

### Actor-Scoped Work

- Every operational workspace supports actor scoping.
- If an actor is locked in the request context, the user works inside that
  locked scope until they leave it.
- The operational question is always:
  who is the active actor for this workspace right now?

### Request Ownership

- `Requests` is owner-isolated.
- A request actor sees only their own requests.
- Same-role visibility is not the default model in this layer.

### Role and Delegation Gating

- `Approvals`, `Dispatch`, and parts of `Operations` are role-gated.
- `Approvals` also supports active delegation.
- A user may act directly or on behalf of a delegator, but only when the
  current stage permits it.

### State Before Action

- Every action in the system is state-gated.
- Users do not perform generic actions; they perform actions only when the
  current state allows them.
- Most operational errors are therefore not data-entry errors, but
  state-transition violations.

### Official Output Model

- `Approvals` does not dispatch.
- `Dispatch` does not approve.
- `Operations` does not create outgoing letters.
- `Intake` does not perform downstream business decisions.

That separation is intentional and is already reflected in the implemented
services and page boundaries.

## Role Matrix

## Intake Operator

| Daily Goal | Trigger | Main Decisions | Allowed Actions | Main Output | Main Hand-Off | Main Exceptions |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Convert an official scanned document into trusted system data | A new scanned guarantee, amendment, extension notice, release note, status letter, or attachment arrives | Which scenario applies, which extracted values are trusted, which fields need explicit human review, which form profile the document belongs to | upload, extract, review, correct, save | staged extraction becomes a saved intake submission tied to a guarantee/document record | documentary truth becomes available to downstream users | no eligible actor, missing file, extraction failure, invalid confirmed fields |

### Intake Daily Logic

- The intake user works one document scenario at a time.
- The core daily cycle is:
  upload -> extract -> review -> save
- Review is not optional when confidence is low or the field is marked as
  critical.
- The intake user is expected to finish the document and stop there.

They do not:

- submit outgoing business requests
- approve requests
- dispatch letters
- apply bank-response business effects

## Request Owner

| Daily Goal | Trigger | Main Decisions | Allowed Actions | Main Output | Main Hand-Off | Main Exceptions |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Create and manage outgoing business actions against owned guarantees | The user needs a release, extension, reduction, replacement, or status action on an owned guarantee | Which request type applies, whether amount/expiry is required, whether the draft is ready for approval, whether to revise, cancel, or withdraw | create, edit draft, edit returned request, submit for approval, cancel, withdraw from approval | a request in `Draft`, `Returned`, `InApproval`, or later lifecycle states | once submitted, the request enters `Approvals` | no eligible actor, invalid workflow template, non-editable state, non-withdrawable state |

### Request Daily Logic

- The request owner works inside an owned list only.
- The normal path is:
  create -> refine -> submit
- A returned request comes back to the same owner for revision.
- A request in approval may be withdrawn only while the approval process is
  still in progress.
- A request can be cancelled only before it becomes an active downstream item.

### Request State Expectations

| State | Daily Meaning | Typical Next Action |
| :--- | :--- | :--- |
| `Draft` | user is still composing the action | submit or cancel |
| `Returned` | approver sent it back for revision | revise and resubmit |
| `InApproval` | approvers are acting on it | monitor or withdraw |
| `ApprovedForDispatch` | signing chain finished | dispatch |
| `AwaitingBankResponse` | outgoing letter was sent and now the external bank must respond | wait for dispatch proof or operations intake |
| `Completed` | downstream loop closed | none |
| `Rejected` | approval chain stopped the request | none |
| `Cancelled` | owner closed it before completion | none |

## Approver

| Daily Goal | Trigger | Main Decisions | Allowed Actions | Main Output | Main Hand-Off | Main Exceptions |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Move actionable requests through the configured approval and signing chain | A request reaches a stage assigned to the approver's direct role or delegated role | approve, return, or reject; note whether governance blocks the action; confirm whether acting directly or on behalf of another signer | approve, return, reject, open dossier | approval ledger progression and next workflow state | fully approved items move to `Dispatch`; returned items go back to `Requests` | no eligible actor, request not actionable, governance block, delegation mismatch |

### Approval Daily Logic

- The approver queue is not owner-based; it is stage-and-role based.
- A user may see work because:
  - they own the required role directly
  - they are actively delegated for that role
- A decision writes approval history and may transition the request
  immediately.
- `Return` is not a comment-only action; it sends the request back for revision.
- `Reject` closes the approval path for that request.
- `Approve` either advances to the next stage or completes the signing chain.

### Approval Reality To Validate

The implemented model is strong on:

- delegation
- governance blocking
- ledger capture
- stage progression

The part still best validated with real users is:

- how decision makers actually read the dossier before deciding
- how much evidence they need at the moment of decision

## Dispatch Operator

| Daily Goal | Trigger | Main Decisions | Allowed Actions | Main Output | Main Hand-Off | Main Exceptions |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Turn approved outgoing requests into recorded dispatch events and confirmed deliveries | A request reaches `ApprovedForDispatch` | print mode, dispatch channel, reference/date consistency, whether to confirm delivery now or later, whether a dispatch event needs reopening | print, record dispatch, confirm delivery, reopen dispatch | outgoing correspondence and dispatch proof | dispatched requests move into `AwaitingBankResponse`; completed delivery waits for incoming bank response or later correction | missing permissions, missing reference/date, request not ready, delivery not pending, reopen note required |

### Dispatch Daily Logic

- Dispatch starts only after approvals finish.
- Printing and dispatch are separate actions.
- Delivery confirmation is also separate from dispatch recording.
- Reopen exists because dispatch data may need correction after the fact.

This means the daily operational path is normally:

approve complete -> print -> record dispatch -> confirm delivery -> wait for bank response

### Dispatch Reality To Validate

The current implementation is structurally sound, but the following usually
vary in real environments and should be validated with operators:

- whether print always precedes dispatch
- which channels are actually used most often
- how delivery proof is recorded in practice
- whether one operator or multiple operators share the same dispatch chain

## Operations Reviewer

| Daily Goal | Trigger | Main Decisions | Allowed Actions | Main Output | Main Hand-Off | Main Exceptions |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Apply the effect of an incoming bank response onto the correct request and guarantee | An incoming bank response or related document has already been captured and routed for review | which existing request it matches, whether the scenario is compatible, whether confirmed amount/expiry/replacement values are present, whether a completed application must be reopened for correction | review candidates, apply bank response, reopen applied response | guarantee and request state updated from external bank evidence | closed external loop returns the guarantee/request to their next stable state | no eligible actor, request mismatch, bank profile mismatch, missing confirmed values, reopen not allowed |

### Operations Daily Logic

- Operations is the inward-facing mirror of dispatch.
- Dispatch sends the official request outward.
- Operations receives the documentary response and applies its business effect.

The implemented daily path is:

incoming response captured -> review open item -> choose matching request ->
apply bank confirmation -> mark item completed

The correction path is:

completed application found wrong -> reopen applied response -> return item to
correction state -> reapply correctly

### Operations Reality To Validate

This area is the most likely to differ from field practice.

Specifically validate:

- whether users start from the document or from the guarantee/request
- how often matching is obvious vs manually reasoned
- whether operators think in terms of "response lanes" or simply "document type"

## Administrator

| Daily Goal | Trigger | Main Decisions | Allowed Actions | Main Output | Main Hand-Off | Main Exceptions |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Keep the operating model usable and governed | New users, changed roles, changed signer chains, or changed delegation periods | which users can act, which roles they hold, who may delegate, what the approval path should be for each request type/category combination | create users, set passwords, create roles, assign permissions, create/revoke delegations, modify workflow stages/governance | operating capability for every other workspace | downstream workspaces immediately reflect those definitions | invalid workflow definitions, missing role assignments, expired or revoked delegations |

### Administration Daily Logic

- Administration is not the business center of the product.
- It is the control layer that keeps the operating model valid.
- Workflow changes are especially sensitive because they change who can approve
  and in what order.

## End-To-End Daily Scenarios

## Scenario A: New Incoming Documentary Truth

1. Intake captures and verifies a new document.
2. The guarantee record or its documentary state is updated.
3. Downstream users now work from that saved truth, not from the raw scan.

## Scenario B: Outgoing Request To Bank

1. Request owner creates a request on an owned guarantee.
2. Request enters approval chain.
3. Approvers sign/approve in order.
4. Dispatch prints and records the outgoing letter.
5. Request waits for external bank response.

## Scenario C: Incoming Bank Response To Existing Request

1. Intake captures the incoming bank response.
2. Operations reviews the response item.
3. Operations selects or confirms the matching request.
4. Operations applies the bank confirmation.
5. Guarantee and request status move to their next resolved state.

## Scenario D: Approval Revision Loop

1. Request owner submits.
2. Approver returns with note.
3. Request owner revises.
4. Request owner resubmits.

## Scenario E: Correction After Downstream Action

1. Dispatch or Operations action was recorded.
2. Operator discovers a correction issue.
3. Reopen action is used with explicit note.
4. Item returns to a workable state.
5. Correct downstream action is applied again.

## High-Confidence Areas

These parts are strongly backed by live code and tests:

- request ownership isolation
- role/delegation-based approvals
- approval governance blocking
- draft/returned/approval/dispatch state transitions
- dispatch print/record/confirm/reopen path
- operations apply/reopen path
- production and testing guardrails around the system

## Live Validation Areas

These parts should still be validated against real daily users:

- batching behavior in intake
- real dossier reading order in approvals
- real match-selection habits in operations
- real proof-of-dispatch and proof-of-delivery practice
- whether some users effectively perform multiple roles in one sitting

## Practical Reading Rule

If you need to understand the current daily operating logic of the system, read
in this order:

1. [ARCHITECTURE.md](ARCHITECTURE.md)
2. this file
3. [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
4. the live Razor Pages and Application services for the affected workspace

If the implemented code and real operator practice ever diverge, update this
file after the validation result is confirmed.
