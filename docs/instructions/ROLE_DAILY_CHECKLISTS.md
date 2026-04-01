# BG Role Daily Checklists

## Document Role

- Status: `source of truth`
- Scope: concise daily execution checklists for each active role
- Documentation index: [README.md](../README.md)
- Related documents:
  - [ARCHITECTURE.md](ARCHITECTURE.md)
  - [DAILY_OPERATIONS_MATRIX.md](DAILY_OPERATIONS_MATRIX.md)
  - [PRODUCTION_RUNBOOK.md](PRODUCTION_RUNBOOK.md)

## Boundary

This file is the short-form operating companion to
[DAILY_OPERATIONS_MATRIX.md](DAILY_OPERATIONS_MATRIX.md).

Use this file when you need:

- a quick daily checklist
- a role-by-role operating routine
- a handoff-oriented view of the system

Use the matrix file when you need the deeper decision model, state logic, or
validation boundary.

## Intake Operator

### Start Of Shift

- Confirm you are working under the correct actor context.
- Confirm the incoming document belongs to the correct intake scenario.
- Confirm the document is readable and complete before extraction.

### For Each Document

- Upload the scanned document.
- Run extraction.
- Review all critical fields first.
- Verify document form and bank profile selection.
- Correct low-confidence or explicitly flagged values.
- Confirm the final verified values before save.
- Save only when the document and extracted values agree.

### Before Handoff

- Confirm the document is no longer just staged.
- Confirm the guarantee/document record now reflects the saved truth.
- Confirm any downstream user can work from the saved version without reopening the scan.

### Stop And Escalate When

- no eligible actor is available
- the file is missing or unreadable
- extraction fails
- a critical field cannot be confidently verified
- the document scenario itself is unclear

## Request Owner

### Start Of Shift

- Confirm you are viewing your own actor scope.
- Review owned requests first, not all guarantees.
- Separate drafts, returned requests, and in-approval requests mentally before acting.

### For A New Request

- Select the correct guarantee.
- Choose the correct request type.
- Enter amount or expiry only when that request type requires it.
- Add notes only where they materially clarify the action.
- Save the request as a draft only if it still needs refinement.
- Submit once the request is ready for approval.

### For A Returned Request

- Read the return context first.
- Revise only the fields relevant to the return.
- Resubmit after the correction is complete.

### For An In-Approval Request

- Monitor the current stage.
- Withdraw only if the business action must stop before approval completes.

### Before Handoff

- Confirm the request state is the one you intended:
  - `Draft`
  - `Returned`
  - `InApproval`
  - `Cancelled`

### Stop And Escalate When

- no eligible actor is available
- the request is not editable
- the workflow template is missing or invalid
- the request is no longer withdrawable or cancellable in its current state

## Approver

### Start Of Shift

- Confirm the active actor and whether you are acting directly or through delegation.
- Review blocked items separately from actionable items.
- Open the dossier only when the queue panel is not enough for a safe decision.

### For Each Actionable Item

- Confirm the request and requested change.
- Confirm the current stage and signer context.
- Check whether governance is blocking the decision.
- Decide one of three outcomes only:
  - approve
  - return
  - reject
- Add note text when the decision requires explanation or traceability.

### Before Handoff

- Confirm the item actually left your actionable state.
- Confirm whether it advanced, returned to owner, or closed by rejection.

### Stop And Escalate When

- no eligible actor is available
- the request is not actionable for your current role
- governance blocks the decision
- delegation context appears wrong
- the dossier evidence is insufficient for a safe decision

## Dispatch Operator

### Start Of Shift

- Confirm the active actor and available dispatch permissions.
- Separate ready-to-dispatch items from pending-delivery items.
- Confirm which channels you are allowed to use today.

### For Each Ready Item

- Confirm the request is fully approved for dispatch.
- Print the outgoing letter when printing is required.
- Confirm reference number and letter date before dispatch recording.
- Record dispatch using the correct channel.

### For Pending Delivery

- Confirm the correspondence item is the right one.
- Record delivery evidence and delivery note.
- Confirm delivery only when the external handoff actually occurred.

### Correction Path

- Reopen dispatch only with a clear correction note.
- Reapply the corrected dispatch or delivery data after reopening.

### Before Handoff

- Confirm the item moved to the intended state:
  - still ready
  - dispatched
  - pending delivery
  - awaiting bank response

### Stop And Escalate When

- the request is not ready for dispatch
- reference number or letter date is missing
- channel permission is missing
- delivery is not actually pending
- correction note is missing for reopen

## Operations Reviewer

### Start Of Shift

- Confirm the active actor.
- Review open items before recently completed items.
- Separate obvious matches from ambiguous matches.

### For Each Open Item

- Confirm the incoming document scenario.
- Confirm the response belongs to the same bank profile where required.
- Review suggested request matches.
- Select the correct request manually if needed.
- Enter confirmed expiry, amount, or replacement number when the scenario requires it.
- Apply the bank response only after the request match and confirmed values are sound.

### Correction Path

- Use reopen only for already applied items that require correction.
- Record a correction note before reopening.
- Re-review and reapply after reopening.

### Before Handoff

- Confirm the review item is now either:
  - still open
  - completed correctly
  - reopened for correction

### Stop And Escalate When

- no eligible actor is available
- the response does not match any valid request
- the bank profile is incompatible
- confirmed values required by the scenario are missing
- reopen is attempted on a non-completed item

## Administrator

### Start Of Shift

- Review whether any access, role, delegation, or workflow changes are waiting.
- Treat workflow edits as production-sensitive changes even in routine admin work.

### Users And Roles

- Create users only with the minimum required role set.
- Set or reset passwords only for explicit operational need.
- Confirm role assignments after any change.

### Delegations

- Confirm delegator, delegate, role, start, and end dates together.
- Revoke delegations explicitly when they are no longer valid.

### Workflow Administration

- Confirm the request type and guarantee category before editing a workflow.
- Confirm every stage has the intended role and order.
- Confirm governance settings match the intended approval policy.

### Before Handoff

- Confirm admin changes are deliberate, not exploratory.
- Confirm no partial workflow edit was left in an inconsistent state.

### Stop And Escalate When

- a workflow becomes operationally incomplete
- a required role does not exist
- a delegation window is ambiguous
- a production access change lacks approval

## Home Dashboard Reading Routine

Use the home page to decide where to go next, not to complete work itself.

Read it in this order:

1. what is waiting now
2. which workspace owns that waiting work
3. whether the next action belongs to you or another role

Do not treat the home page as the system of record for detailed decisions.

## Validation Rule

If daily user observation proves that one of these checklists does not match the
real operating routine, update:

1. [DAILY_OPERATIONS_MATRIX.md](DAILY_OPERATIONS_MATRIX.md)
2. this file

in the same change.
