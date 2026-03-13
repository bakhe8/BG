# 2026-03-12 UX Baseline Matrices

Purpose:
- تثبيت baseline تنفيذي لمرحلة `UX-0`.
- تعريف حدود كل شاشة قبل تعديلها.
- منع عودة تضخم المعلومات أو تكرار surfaces بدون مالك واضح.

Companion documents:
- `docs/audits/2026-03-12-ux-remediation-plan.md`
- `docs/audits/2026-03-12-ux-execution-backlog.md`

## 1. Screen Purpose Matrix

| Surface | Dominant task | Supporting context | Audit/reference context | Must not dominate the screen |
| --- | --- | --- | --- | --- |
| `Intake/Workspace` | capture and save one document package | selected scenario, required review fields, extraction confidence, handoff outcome | pipeline notes, future scanner notes, quality gates, operator scope | system architecture narrative |
| `Operations/Queue` | close one review item by applying the correct operational response | match suggestions, capture provenance, recommended lane | workflow template reference | template reference inside live action flow |
| `Requests/Workspace` | create a request and track owned requests at summary level | workflow preview before submission, current stage, latest decision | full ledger history | full timeline in every request card |
| `Approvals/Queue` | decide on a request at the current approval stage | requester, stage, blocker, delegation, concise next step | prior signatures, attachments, full timeline | dossier-level evidence inside queue item |
| `Approvals/Dossier` | inspect full evidence and history for one approval item | governance narrative, attachments, signatures, timeline | full ledger policy context | inline decision execution |
| `Dispatch/Workspace` | print, dispatch, and confirm delivery | current dispatch state, ready timestamp, reference data | historical print metadata | mixed execution plus verbose lifecycle narrative |
| `Administration/Users` | create users and maintain credentials | roles, credential readiness | account timestamps | treating user list as full audit report |
| `Administration/Roles` | define roles and permission bundles | grouped permissions | raw permission key reference | long permission detail without task grouping |
| `Administration/Delegations` | create and revoke temporary approval delegations | active status, effective period, role | revocation history | burying create/revoke intent under duplicate metadata |
| `Administration/Workflow` | configure executable approval paths | integrity status, stage order, governance rules | full operational explanation of every downstream state | turning definition editor into runtime dossier |

## 2. Object Ownership Matrix

| Object | Summary surfaces | Canonical detail owner | Audit-only layers |
| --- | --- | --- | --- |
| `GuaranteeRequest` | `Requests/Workspace`, `Approvals/Queue`, `Dispatch/Workspace` | `Approvals/Dossier` for approval dossier, `Requests/Workspace` owner summary for owned status | ledger timeline, prior signatures, policy context |
| `GuaranteeDocument` | `Intake/Workspace`, `Approvals/Dossier` attachment summary | `Approvals/Dossier` when document is evidence for approval | capture provenance source metadata |
| `OperationsReviewItem` | `Operations/Queue` | `Operations/Queue` | capture provenance, match confidence reasoning |
| `Approval item` | `Approvals/Queue` | `Approvals/Dossier` | prior signatures, governance details, full timeline |
| `Dispatch correspondence` | `Dispatch/Workspace` | `Dispatch/Workspace` pending delivery item | print/dispatched/delivery evidence trail |
| `Role` | `Administration/Roles`, `Administration/Workflow` | `Administration/Roles` | permission key inventory |
| `Approval delegation` | `Administration/Delegations`, `Approvals/Queue` contextual mention | `Administration/Delegations` | revocation rationale/history |

## 3. Disclosure Classification Matrix

### Action-critical
- actor context needed to execute the action
- current stage or current queue reason
- request type / scenario / dispatch state
- required fields to save or submit
- current blocker when an action is blocked
- primary action and its required inputs

### Confidence-supporting
- selected workflow preview before submit
- match suggestions and scores
- requested amount / expiry / bank reference
- delegation visibility
- last decision summary
- print count / last print mode when dispatching

### Audit-only
- full ledger timeline
- prior signatures with full actor chain
- attachment capture provenance details
- pipeline architecture notes
- future integration notes
- full governance policy metadata

Rule:
- audit-only content may stay reachable, but must not sit at the same visual priority as the primary action.

## 4. Page Complexity Thresholds

Thresholds for operational surfaces:

- maximum dominant sections above the fold: `3`
- maximum inline actions per entity card: `3 primary groups`
- maximum metadata layers inside one action card: `2`
  - summary data
  - confidence/supporting context
- full history/timeline inside a live action card: `not allowed`
- full evidence attachments inside a live action card: `not allowed`

Trigger review if any of these happen:
- a queue item needs scrolling to discover the primary action
- the same entity card includes both execution controls and full audit history
- a page adds a new section without classifying it as `action-critical`, `confidence-supporting`, or `audit-only`

## 5. UX Guardrails For Implementation

Before adding any new surface or section:
1. state the dominant task
2. state the primary user
3. state the canonical detail owner for the object
4. classify each field group by disclosure level

Stop condition:
- if a new requirement can only be met by injecting more provenance, history, or policy data directly into the live action card, the change must be reviewed before implementation.
