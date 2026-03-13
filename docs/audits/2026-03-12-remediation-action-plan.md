# 2026-03-12 Remediation Action Plan

Purpose:
- تحويل نتائج:
  - `mid-development architecture audit`
  - `technical debt audit`
  - `cognitive system audit`
  إلى خطة تنفيذية قصيرة المدى تمنع الانحراف وتخفض الديون قبل التوسع.

Source reports:
- `docs/audits/2026-03-12-mid-development-architecture-audit.md`
- `docs/audits/2026-03-12-technical-debt-audit.md`
- `docs/audits/2026-03-12-cognitive-system-audit.md`

## 2026-03-13 Status Refresh

This plan should now be treated as a **historical foundation-closure plan**.

Its three original tracks were created to close structural engineering risks:

- `Sprint 1`: Trust Boundary
- `Sprint 2`: Integrity and Canonical Truth
- `Sprint 3`: Runtime Readiness and Scale Posture

Those tracks are now materially addressed and no longer define the immediate
next execution step for the product.

The active next-step direction has moved to:

- [frontend_reconstruction_plan.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/frontend_reconstruction_plan.md)
- [2026-03-12-master-program-execution-plan.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/audits/2026-03-12-master-program-execution-plan.md)
- [2026-03-13-component-role-visibility-matrix.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/audits/2026-03-13-component-role-visibility-matrix.md)

Important update:

- `docs/ui-proposals` is now an essential planning input for frontend work.
- It should not be treated as optional inspiration outside the main execution
  plans.

## Executive Decision

لا نوسّع الـ business features الآن.

القرار المعتمد لهذه المرحلة:
- `Sprint 1`: Trust Boundary
- `Sprint 2`: Integrity and Canonical Truth
- `Sprint 3`: Runtime Readiness and Scale Posture

ولا يبدأ Sprint لاحق قبل إغلاق exit criteria الخاصة بالسابق.

## Stop Rules

يوقف مؤقتًا حتى نهاية `Sprint 2`:
- أي features جديدة في workflows أو approvals أو dispatch
- أي شاشة إدارية جديدة
- أي API جديد دون authorization policy
- أي scenario intake جديد
- أي state/status جديد في الطلبات أو الضمانات

يوقف مؤقتًا حتى نهاية `Sprint 3`:
- أي ربط ذكي أو auto-matching أعلى من rule-based assist
- أي machine learning أو confidence automation
- أي integration خارجية جديدة غير مطلوبة لإغلاق المخاطر الحالية

## Global Engineering Rules For The Next 3 Sprints

1. لا يضاف controller أو page handler جديد دون authorization rule صريح.
2. لا يضاف scenario جديد كسلسلة نصية خام خارج مصدر مركزي typed.
3. لا يضاف mapping جديد من enum/status إلى resource key داخل خدمة عشوائية.
4. لا تُفعل workflow definition إذا احتوت stage بلا role.
5. لا يُسمح بخدمة application جديدة تتجاوز `200-250` سطرًا دون تقسيم مبرر.
6. أي إصلاح في auth, workflow validity, أو queue behavior يجب أن يصاحبه regression test.

## Sprint 1: Trust Boundary

Goal:
- تحويل النظام من "session identity picker" إلى "trusted actor boundary".

Priority:
- `P0`

Why first:
- كل شيء بعده يعتمد على أن اسم المنفذ في ledger والموافقات يعكس هوية موثوقة.

### Scope

- local authentication حقيقي
- authorization policies موحدة
- حماية الـ admin APIs
- تقوية session model

### Work Items

1. Introduce local credential model
   - إضافة primitive حقيقية للاعتماد:
     - password hash
     - password salt أو built-in hasher metadata
     - password-set timestamp
     - optional reset-required flag
   - ربطها بالمستخدم المحلي فقط

2. Replace user-selection sign-in
   - إلغاء sign-in المبني على اختيار `UserId`
   - اعتماد username/password
   - منع انتحال أي مستخدم نشط

3. Introduce explicit authorization policies
   - policies لكل areas الأساسية:
     - `users.manage`
     - `roles.manage`
     - `delegations.manage`
     - `workflow.manage`
     - queue/view/manage/sign policies
   - منع الاعتماد على path-based middleware وحده

4. Protect admin APIs and administration pages
   - controllers
   - Razor Pages administration endpoints
   - POST handlers

5. Session and auth hardening
   - cookie settings review
   - sign-out behavior
   - invalid user session handling
   - unauthorized vs forbidden behavior

6. Test coverage
   - hosted integration tests for:
     - valid sign-in
     - invalid sign-in
     - protected admin API access
     - protected page access
     - forbidden role behavior

### Deliverables

- real local sign-in flow
- protected admin API surface
- reusable authorization policy map
- integration tests for auth boundary

### Exit Criteria

- لا يمكن تسجيل الدخول كمستخدم دون credentials
- كل `api/admin/*` endpoints محمية صراحة
- صفحات الإدارة محمية صراحة
- `dotnet test` يشمل integration tests تمر لهذا السلوك
- لا يبقى أي endpoint إداري يعتمد فقط على `WorkspaceAccessMiddleware`

### Do Not Carry Forward

- لا يبقى أي code path يقرأ user identity من form selection كحقيقة نهائية
- لا تبقى أي صفحة "sign in" تعرض قائمة المستخدمين للتقمص المباشر

## Sprint 2: Integrity and Canonical Truth

Goal:
- جعل النظام يملك مصدر حقيقة واحدًا وقابلًا للتحقق لكل:
  - workflow definitions
  - intake scenarios
  - UI/resource projections

Priority:
- `P1`

Why second:
- بعد تثبيت الهوية، الخطر التالي هو أن يصبح النظام صحيح الأمان لكنه هش المعنى أو غير صالح تشغيليًا.

### Scope

- workflow integrity rules
- canonical metadata
- typed scenario identifiers
- centralized projection/mapping rules

### Work Items

1. Workflow validity model
   - منع تفعيل definition إذا:
     - لا تحتوي stages
     - stage بلا role
     - sequence مكسور
   - إدخال lifecycle واضح:
     - `Draft`
     - `Active`
     - `Archived`

2. Fix workflow bootstrap model
   - إيقاف seed الذي يزرع active stages بـ `roleId: null`
   - إما:
     - seed draft definitions
     - أو seed active only when role mapping exists

3. Introduce typed intake scenario key
   - إزالة الاعتماد على raw strings مثل:
     - `new-guarantee`
     - `extension-confirmation`
     - `reduction-confirmation`
   - بناء source مركزي واحد للسلوك والخصائص

4. Centralize projection mappings
   - request type -> resource key
   - status -> resource key
   - category -> resource key
   - capture channel -> resource key
   - نقلها إلى layer/shared projector واحد

5. Reduce split truth
   - database becomes the only runtime source for workflow definitions
   - catalog يبقى seed/bootstrap only, not parallel runtime logic

6. Improve ledger semantics
   - تثبيت `event type + structured payload` كمصدر الحقيقة
   - البدء في فصل summary rendering عن domain event generation

### Deliverables

- workflow validator
- typed scenario model
- centralized presentation/projector layer
- cleaned runtime source-of-truth boundaries

### Exit Criteria

- لا يمكن حفظ workflow active غير صالح
- لا توجد raw scenario strings متناثرة في web/application logic إلا داخل المصدر المركزي
- كل enum/status/category projections تمر عبر mapper مركزي واحد
- runtime workflow resolution يعتمد على database truth فقط

### Do Not Carry Forward

- no roleless active stages
- no duplicate mappings across services
- no new feature using raw scenario string literals

## Sprint 3: Runtime Readiness and Scale Posture

Goal:
- نقل النظام من "correct foundation" إلى "operationally sustainable foundation".

Priority:
- `P1/P2`

Why third:
- بعد تثبيت الثقة وسلامة الحقيقة، يأتي تخفيض مخاطر الأداء والتوسع والتشغيل.

### Scope

- extraction architecture
- queue/read-model shaping
- performance posture
- broader end-to-end confidence

### Work Items

1. Replace extraction stub architecture
   - تثبيت interfaces/adapters لـ:
     - document classification
     - direct PDF text extraction
     - OCR fallback
     - confidence scoring
     - field provenance
   - `LocalIntakeExtractionEngine` يجب أن يصبح orchestrator أو test adapter فقط

2. Introduce queue read models
   - pagination for:
     - operations queue
     - approvals queue
     - requests workspace list
     - dispatch queue
   - summary DTO projections from persistence layer بدل تحميل aggregate graphs كاملة

3. Performance and data-shape cleanup
   - مراجعة eager loading الحالي
   - تقليل graph loading غير الضروري
   - فصل listing queries عن aggregate mutation loads

4. Integration test expansion
   - hosted end-to-end flow minimum:
     - sign-in
     - create request
     - submit for approval
     - approve
     - dispatch
     - apply bank response

5. Operational observability baseline
   - structured logging around:
     - auth failure
     - workflow invalidity
     - queue actions
     - bank response application failure

### Deliverables

- non-stub extraction architecture boundary
- paged queues
- read-model queries
- end-to-end hosted test coverage
- observability baseline

### Exit Criteria

- queues no longer depend on full aggregate loading for listing
- extraction pipeline is no longer heuristic-only demo logic
- one hosted end-to-end operational flow passes automatically
- operational failure points emit actionable logs

### Do Not Carry Forward

- no expansion into ML auto-linking before this sprint closes
- no assumption that current queue loading shape will scale

## Recommended Implementation Order Inside Each Sprint

### Sprint 1 order

1. credential model
2. sign-in rewrite
3. authorization policies
4. controller/page protection
5. integration tests

### Sprint 2 order

1. workflow validator
2. workflow activation lifecycle
3. typed scenario key
4. centralized mapping layer
5. ledger payload/projection split

### Sprint 3 order

1. extraction adapters/contracts
2. queue read-model design
3. pagination and query shaping
4. hosted end-to-end tests
5. logging/observability

## What We Explicitly Defer

Deferred until after these sprints:
- ML-driven bank-response matching
- Oracle sync
- scanner hardware integration
- email dispatch integration
- digital signature pipeline replacement
- advanced analytics dashboards

Reason:
- هذه كلها ستتأثر مباشرة إن لم نثبت الثقة، وصحة التعريفات، وشكل runtime foundation أولًا.

## Definition Of Resume For Feature Expansion

يسمح باستئناف التوسع الوظيفي فقط إذا تحقق التالي:

- `Sprint 1` و`Sprint 2` مكتملان بالكامل
- `Sprint 3` على الأقل مكتمل جزئيًا في:
  - extraction boundary
  - queue read models
  - hosted end-to-end test baseline

## Immediate Recommendation

This plan no longer defines the immediate next implementation step.

Its purpose now is historical: to document the engineering foundation work that
had to close before broader product expansion continued.

The current next-step reference is:

- [2026-03-12-master-program-execution-plan.md](/C:/Users/Bakheet/Documents/Projects/BG/docs/audits/2026-03-12-master-program-execution-plan.md)

The active next move is now:

- `institutional shell and shared surface-zone contract`
