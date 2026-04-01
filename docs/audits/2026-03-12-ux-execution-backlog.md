# 2026-03-12 UX Execution Backlog

Purpose:
- تحويل `2026-03-12-ux-remediation-plan.md` إلى backlog تنفيذي قابل للتطبيق.
- ترتيب العمل على مستوى السبرنتات، الشاشات، ومخرجات القبول.
- إبقاء التركيز على تقليل التعقيد البنيوي لا على التحسينات التجميلية.

Companion document:
- `docs/audits/2026-03-12-ux-remediation-plan.md`

## 2026-03-13 Status Refresh

This backlog must now be read together with:

- [README.md](../ui-proposals/README.md)
- [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
- [2026-03-13-component-role-visibility-matrix.md](2026-03-13-component-role-visibility-matrix.md)

Operational update:

- the proposal library is now an essential reference for the remaining UX work
- the main operational surfaces have already undergone a first structural
  simplification pass
- the active UX step is no longer only `Approvals` or `Intake` in isolation
- the active UX step is now the shared composition layer:
  - institutional shell
  - role-aware navigation
  - `Main / Support Rail / Detail Drawer / Dossier` zoning

## Execution Model

Work structure:
- `UX-0`: Baseline and guardrails
- `UX-1`: Approvals
- `UX-2`: Intake
- `UX-3`: Requests
- `UX-4`: Operations + Dispatch
- `UX-5`: Administration + system-wide interaction contract

Execution rule:
- لا يبدأ track لاحق قبل إغلاق acceptance criteria الخاصة بالذي قبله
- لا تخلط الأعمال البصرية التفصيلية مع إعادة ضبط المعنى البنيوي
- أي تعديل على شاشة يجب أن يثبت:
  - ما المهمة الأساسية؟
  - ما المعلومات الضرورية للفعل؟
  - ما الذي يجب أن ينتقل إلى سطح مرجعي أو تفصيلي؟

## Global UX Guardrails

These apply to every sprint:

1. لا تضاف section جديدة داخل شاشة تشغيلية قبل تصنيفها:
   - `essential`
   - `supporting`
   - `audit`
2. لا يعرض نفس object كاملًا في أكثر من surface.
3. لا تجتمع في block واحد:
   - `action`
   - `full history`
   - `full governance narrative`
4. أي blocked action يجب أن يجيب ضمنيًا على:
   - لماذا مُنع؟
   - ماذا أفعل الآن؟
5. أي list item يتجاوز حد “ملخص عملي” يجب أن يتحول إلى detail surface.

## UX-0: Baseline and Guardrails

Goal:
- تثبيت baseline منهجي قبل تعديل الشاشات.

Priority:
- `P0`

### Scope

- تعريف dominant task لكل شاشة
- تعريف مستويات الإفصاح
- تعريف surfaces المرجعية للكائنات

### Backlog Items

1. Produce screen purpose matrix
   - `Intake`
   - `Operations`
   - `Requests`
   - `Approvals`
   - `Dispatch`
   - `Workflow Admin`
   - `Users/Roles/Delegations`

2. Produce object ownership matrix
   - `Guarantee`
   - `GuaranteeRequest`
   - `GuaranteeDocument`
   - `OperationsReviewItem`
   - `Approval item`
   - `Dispatch correspondence`

3. Produce disclosure classification matrix
   - identify which fields are:
     - action-critical
     - confidence-supporting
     - audit-only

4. Define page complexity thresholds
   - section count
   - inline action count
   - metadata density

### Deliverables

- `screen-purpose matrix`
- `object-surface ownership matrix`
- `disclosure classification matrix`
- `page complexity thresholds`

### Acceptance Criteria

- كل شاشة مذكور لها dominant task واحد
- كل object له canonical detail owner
- كل field group مصنف ضمن disclosure level

## UX-1: Approvals Simplification

Goal:
- إعادة `Approvals` إلى شاشة قرار، لا شاشة dossier كاملة.

Priority:
- `P0`

Why first:
- أعلى شاشة قرارية
- أعلى شاشة تجمع:
  - governance
  - prior signatures
  - attachments
  - timeline
  - action blocking

### Scope

- `Approvals/Queue`
- `Approvals/Dossier`

### Backlog Items

1. Split queue responsibilities
   - تحديد ما الذي يجب أن يبقى في queue item فقط
   - وما الذي يرحّل إلى dossier فقط

2. Normalize decision context
   - current stage
   - role
   - requester
   - request intent
   - decision blocker

3. Define governance explanation contract
   - blocked decision semantics
   - prior signer conflict semantics
   - delegation visibility semantics

4. Reduce audit duplication
   - prior signatures, attachments, timeline, ledger policy context
   - تحديد أيها queue-level وأيها dossier-level

5. Define approval completion language
   - what happens after approve / return / reject
   - shown consistently within the approval surface

### Deliverables

- `Approvals surface contract`
- `Queue vs dossier content split`
- `Blocked decision explanation contract`

### Acceptance Criteria

- queue item لم يعد يحمل dossier كاملة
- dossier أصبحت المرجع الرئيسي للتفاصيل الكاملة
- القرار يمكن فهمه دون قراءة timeline كاملة

## UX-2: Intake Decompression

Goal:
- تقليل الحمل المعرفي في `Intake` دون المساس بصرامة المراجعة.

Priority:
- `P0`

Why second:
- أكثر شاشة مرشحة للإرهاق البصري والتشغيلي على المستخدم المختص.

### Scope

- `Intake/Workspace`

### Backlog Items

1. Separate task zones conceptually
   - source capture
   - reviewed fields
   - review confidence
   - operator boundaries
   - technical/pipeline context

2. Reclassify all current sections
   - ما الذي يخدم save الآن؟
   - ما الذي يخدم الثقة فقط؟
   - ما الذي يخدم التوثيق أو الشرح فقط؟

3. Reduce simultaneous decision load
   - scenario choice
   - file upload
   - provenance
   - extraction review
   - manual verification
   يجب ألا تعمل كلها بنفس الوزن في آن واحد

4. Define extraction review contract
   - what the operator must confirm
   - what the operator may inspect
   - what remains audit-only

5. Review presence of pipeline/future scanner/quality sections
   - confirm whether they belong in runtime operator surface

### Deliverables

- `Intake task-zone map`
- `Intake review contract`
- `section reclassification log`

### Acceptance Criteria

- شاشة الإدخال لم تعد تعرّض المستخدم لكل طبقات النظام بنفس اللحظة
- الحقول الحرجة فقط هي التي تبقى في مركز الانتباه أثناء الحفظ
- الشرح البنيوي للنظام لم يعد يزاحم المهمة الأساسية

## UX-3: Requests Clarification

Goal:
- جعل `Requests` مساحة إنشاء ومتابعة واضحة بدل أن تكون قائمة + ledger-heavy surface.

Priority:
- `P1`

### Scope

- `Requests/Workspace`

### Backlog Items

1. Separate create-request context from request-history context
2. Define owned request summary contract
3. Reclassify ledger inside requests surface
4. Clarify workflow preview role
   - is it pre-submission confidence context
   - or full operational reference
5. Define current-stage visibility rules for request owner

### Deliverables

- `Requests creation vs tracking split`
- `owned-request summary contract`
- `owner-facing state explanation rules`

### Acceptance Criteria

- صاحب الطلب يستطيع فهم وضع طلبه دون قراءة ledger مطولة
- شاشة الإنشاء لا تتنافس مع شاشة التتبع على نفس الانتباه

## UX-4: Operations and Dispatch Alignment

Goal:
- ضبط شاشات handoff حتى تبقى تشغيلية ولا تتحول إلى surfaces تفسيرية متضخمة.

Priority:
- `P1`

### Scope

- `Operations/Queue`
- `Dispatch/Workspace`

### Backlog Items

1. Clarify `Operations` item contract
   - suggested matches
   - captured provenance
   - action form
   - workflow templates block

2. Decide whether workflow templates belong in live queue surface
3. Define dispatch execution states clearly
   - ready
   - printed
   - dispatched
   - pending delivery
4. Normalize handoff explanation
   - why this item is here
   - what completes it
   - what queue owns it next

### Deliverables

- `Operations queue contract`
- `Dispatch state explanation contract`
- `handoff explanation rules`

### Acceptance Criteria

- `Operations` تعرض العمل الحالي بوضوح دون مزاحمة مرجعية غير لازمة
- `Dispatch` تشرح الحالة الحالية والمرحلة التالية بوضوح ثابت

## UX-5: Administration and System-wide Interaction Contract

Goal:
- منع تضخم التعقيد الإداري مع توسع المسارات والأدوار والسياسات.

Priority:
- `P1/P2`

### Scope

- `Workflow Admin`
- `Users`
- `Roles`
- `Delegations`
- shared patterns across the application

### Backlog Items

1. Define administration mental model
   - users
   - roles
   - permissions
   - delegations
   - workflow governance

2. Define admin task boundaries
   - configuration
   - review
   - maintenance

3. Normalize system-wide interaction contracts
   - create
   - edit
   - confirm
   - revoke
   - submit
   - blocked action

4. Normalize notification semantics
   - success
   - warning
   - validation
   - access/governance notice

### Deliverables

- `administration task model`
- `system-wide interaction contract`
- `notification semantics matrix`

### Acceptance Criteria

- الإدارة لا تعتمد على نفس أنماط الكثافة غير المضبوطة الموجودة في الشاشات التشغيلية
- أفعال النظام الأساسية موحدة دلاليًا عبر الوحدات

## Suggested Sprint Sequence

### Sprint UX-A

- `UX-0`
- begin `UX-1`

Exit:
- matrices complete
- Approvals split contract decided

### Sprint UX-B

- finish `UX-1`
- execute `UX-2`

Exit:
- Approvals no longer screen-as-dossier
- Intake no longer screen-as-everything

### Sprint UX-C

- `UX-3`
- begin `UX-4`

Exit:
- Requests clarified
- Operations/Dispatch contract defined

### Sprint UX-D

- finish `UX-4`
- `UX-5`

Exit:
- interaction contracts and admin model stabilized

## Dependency Notes

- `UX-1` depends on dossier already existing, which it does
- `UX-2` depends on preserving provenance and confidence semantics, not removing them
- `UX-3` depends on keeping ledger integrity while reducing ledger dominance in the screen
- `UX-4` depends on preserving business handoff correctness
- `UX-5` depends on finalizing interaction semantics learned from the operational screens first

## Definition of Done for Any UX Correction Task

Each task is not done unless:
- dominant task is clearer than before
- at least one layer of cognitive noise was reduced
- object ownership across surfaces became clearer
- no business traceability was lost
- no governance rule became more opaque

## Immediate Recommendation

Start with:
1. `UX-5` shared interaction contract hardening
2. institutional shell + surface-zone implementation
3. then continue any remaining surface refinements under the new composition model

If only one execution package is approved now, it should be:
- `shared shell + drawer/dossier zoning baseline`

Reason:
- this is now the strongest common dependency across all remaining frontend work and the point where role composition and proposal-driven implementation must converge.
