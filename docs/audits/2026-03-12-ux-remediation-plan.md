# 2026-03-12 UX Remediation Plan

Purpose:
- تحويل التشخيص الحالي لحالة UX في `BG` إلى خطة تصحيح بنيوية قبل التوسع أكثر.
- تقليل التعقيد الإدراكي المتراكم دون الدخول الآن في إعادة تصميم بصري تفصيلية.
- حماية النظام من التحول إلى منتج "صحيح وظيفيًا لكنه ثقيل الاستخدام" مع ازدياد المسارات والسياسات.

This document is intentionally:
- not a visual redesign brief
- not a layout proposal
- not a style guide

It is:
- a structural UX correction plan
- an interaction and workflow simplification plan
- a scaling-risk containment plan

## 2026-03-13 Alignment Refresh

This plan must now be read together with:

- [README.md](../ui-proposals/README.md)
- [frontend_reconstruction_plan.md](../frontend_reconstruction_plan.md)
- [2026-03-13-component-role-visibility-matrix.md](2026-03-13-component-role-visibility-matrix.md)

Updated interpretation:

- the proposal images are now essential inspiration inputs
- they should be treated as a component vocabulary and composition reference
- UX correction is no longer only about reducing density
- it is now also about composing the correct components for the correct role in
  the correct zone

This means the UX correction target is:

`institutional shell + role-specific workbench composition + on-demand dossier depth`

Status update:

- the shell and surface-zone contract are now materially implemented
- the primary operational surfaces have already been re-composed on that model
- the current UX phase is no longer broad surface correction
- it has moved to validation and selective refinement

## Source Context

Built from the current product state as observed in:
- `Intake`
- `Operations`
- `Requests`
- `Approvals`
- `Dispatch`
- `Administration`

And from the diagnosed UX risks already observed during development:
- information density inflation
- object representation fragmentation
- governance opacity for end users

## Executive Decision

لا يجب أن يستمر بناء الواجهات التشغيلية بنفس النمط الحالي.

القرار المعتمد:
- يتم التعامل مع UX الآن كطبقة بنيوية مساوية للأمان وسلامة البيانات
- لا يكتفى بإصلاحات موضعية شاشة بشاشة
- التصحيح يجب أن يطال:
  - حدود الشاشات
  - منطق الإفصاح عن التفاصيل
  - نمط تنفيذ الأفعال
  - تفسير الحالات والحوكمة للمستخدم

## The 3 Critical Risks To Correct First

1. `Screen-as-dossier inflation`
   - كل مساحة عمل بدأت تتحول إلى شاشة تنفيذ + شاشة شرح + شاشة تدقيق + شاشة سجل في نفس الوقت.

2. `Object representation fragmentation`
   - نفس الطلب أو المستند أو القرار يظهر بأكثر من تمثيل ذهني عبر أكثر من شاشة بدون حدود صارمة لما يعرض في كل مكان.

3. `Governance opacity at runtime`
   - النظام يطبق سياسات صحيحة، لكن فهم:
     - لماذا ظهر العنصر
     - لماذا اختفى
     - لماذا مُنع القرار
     - ما التالي
     ليس دائمًا واضحًا بالقدر الكافي للمستخدم التشغيلي.

## UX Stop Rules

Until Track 1 and Track 2 are closed:
- لا تُضاف sections جديدة داخل:
  - `Intake`
  - `Requests`
  - `Approvals`
  - `Dispatch`
  - `Operations`
- لا تُعرض provenance أو ledger أو governance metadata إضافية داخل البطاقة الرئيسية إلا عند وجود مبرر تشغيلي مباشر
- لا يُضاف object detail جديد في أكثر من شاشة بدون تعريف صريح:
  - أين يظهر مختصرًا
  - أين يظهر تفصيليًا
  - من هو صاحب الصفحة المرجعية لذلك الكائن

Until Track 3 closes:
- لا يُضاف action pattern جديد مختلف عن الموجود دون توثيق
- لا تُضاف شاشة تنفيذ جديدة تجمع:
  - queue
  - reference material
  - configuration
  في نفس المستوى

## Non-Goals

Out of scope for this phase:
- إعادة تصميم مرئي كامل
- تغيير الثيمات أو الهوية البصرية
- اقتراح layouts نهائية pixel-by-pixel
- إعادة تسمية كل المصطلحات
- إعادة بناء navigation structure كاملة

## Track 1: Information Density and Screen Boundaries

Goal:
- إعادة كل شاشة إلى "مهمة تشغيلية رئيسية" بدل كونها ملفًا كاملاً متعدد الطبقات.

Priority:
- `P0`

Why first:
- هذه هي أكبر مولد مباشر للحمل المعرفي الحالي.

### Problems To Correct

- كثافة `Intake` العمودية وتجاور:
  - الرفع
  - provenance
  - الاستخراج
  - التحقق
  - الجودة
  - الشرح
  في نفس الشاشة
- تضخم بطاقات `Approvals` و`Requests`
- عرض timeline / ledger / signatures / attachments / governance داخل نفس المستوى مع الفعل الأساسي

### Work Items

1. Define primary task per screen
   - كل صفحة تشغيلية يجب أن تملك `single dominant task`
   - وما عداها يصنف كالتالي:
     - supporting context
     - reference context
     - audit context

2. Define disclosure levels
   - تحديد ثلاث طبقات عرض موحدة:
     - `essential for action`
     - `supporting for confidence`
     - `audit for inspection`

3. Assign one canonical detail surface per object
   - الطلب
   - المرفق
   - الموافقة
   - الإرسال
   - عنصر التشغيل
   يجب أن يملك كل منها surface مرجعية واحدة فقط للتفصيل الكامل

4. Remove mixed-purpose sections from execution surfaces
   - أي section تعليمي أو مرجعي أو استشرافي لا يخدم القرار الحالي مباشرة يراجع ويعاد تصنيفه

### Exit Criteria

- كل صفحة تشغيلية موصوفة بمهمة رئيسية واحدة
- كل عنصر معروض يملك سببًا واضحًا لوجوده في الصفحة
- لا توجد بطاقة تنفيذية واحدة تحتوي simultaneously على:
  - action
  - full timeline
  - full governance narrative
  - full attachment dossier

## Track 2: Workflow Legibility and Runtime Explainability

Goal:
- جعل منطق النظام مفهومًا للمستخدم أثناء التنفيذ، لا فقط صحيحًا في الخلفية.

Priority:
- `P0`

Why second:
- تعقيد الحوكمة سيكبر سريعًا، وإذا بقي غير مفسر سيتحول إلى "غموض نظامي" حتى لو كانت القواعد نفسها صحيحة.

### Problems To Correct

- ضعف تفسير:
  - لماذا العنصر وصل لهذه الطابور
  - ما المرحلة الحالية
  - ما المطلوب الآن
  - لماذا القرار blocked
  - ما الذي ينتظر handoff التالي
- الانتقال بين الحالات واضح للنظام أكثر من وضوحه للمستخدم

### Work Items

1. Define runtime explanation contract
   - لكل item actionable يجب أن يكون هناك تفسير موحد لـ:
     - why am I seeing this?
     - what is expected now?
     - what happens after I act?

2. Define blocked-state explanation model
   - أي حالة منع أو حجب أو عدم صلاحية يجب أن تفسر ضمن contract موحد

3. Normalize current-stage semantics
   - معنى:
     - `current stage`
     - `current owner`
     - `current blocker`
     - `next state`
   يجب أن يكون متسقًا عبر `Requests`, `Approvals`, `Dispatch`, `Operations`

4. Define handoff visibility rules
   - ما الذي يجب أن يعرفه المستخدم عن المرحلة السابقة
   - وما الذي يجب أن يعرفه عن المرحلة التالية
   - دون تحويل شاشته إلى ملف تاريخي كامل

### Exit Criteria

- كل queue item يحمل تفسيرًا تشغيليًا موحدًا لحالته
- كل blocked action له explanation pattern موحد
- كل workflow transition يصف للمستخدم الحالة التالية بوضوح

## Track 3: Interaction Contract Normalization

Goal:
- توحيد منطق التفاعل عبر الوحدات بدل الاكتفاء بتشابه شكلي جزئي.

Priority:
- `P1`

### Problems To Correct

- أنماط الإنشاء والتنفيذ والتحرير ليست مفككة، لكنها ليست موحدة ذهنيًا بالكامل
- بعض القوائم هي في الواقع forms طويلة
- بعض التفاصيل inline وبعضها منفصل

### Work Items

1. Define create pattern
   - أين يبدأ الإنشاء
   - متى يكون inline
   - متى يحتاج surface مستقلة

2. Define list vs detail contract
   - ما الحد الأقصى المقبول لما يعرض في list item
   - ومتى يصبح المحتوى detail surface

3. Define action execution contract
   - كيف تظهر:
     - primary action
     - secondary actions
     - destructive actions
     - blocked actions

4. Define notification contract
   - success
   - warning
   - validation
   - system explanation
   لا تستخدم بنفس الوزن ولا بنفس الغرض

### Exit Criteria

- يوجد interaction contract موحد للإنشاء والتحرير والتنفيذ
- list items لم تعد تتحول إلى detail surfaces كاملة
- notification semantics أصبحت مصنفة بوضوح

## Track 4: Object Model Clarity Across Screens

Goal:
- تقليل تضارب التمثيل الذهني للكائنات الأساسية في النظام.

Priority:
- `P1`

### Objects In Scope

- `Guarantee`
- `GuaranteeRequest`
- `GuaranteeDocument`
- `Approval decision`
- `Dispatch correspondence`
- `Operations review item`

### Work Items

1. Define canonical UX identity of each object
   - ما هو
   - من يتعامل معه
   - في أي شاشة يظهر مختصرًا
   - في أي شاشة يظهر تفصيليًا

2. Define ownership of details
   - لا يكرر نفس التفصيل الكامل في أكثر من surface

3. Define cross-screen summary rules
   - أي fields تعتبر summary fields
   - أي fields تعتبر audit-only fields

### Exit Criteria

- لكل object canonical detail surface واحدة
- لا تتكرر نفس الطبقات المعلوماتية الكاملة في أكثر من شاشة

## Track 5: Growth Guardrails For Future UX Complexity

Goal:
- منع عودة المشكلة مع كل feature جديدة.

Priority:
- `P1/P2`

### Work Items

1. Add UX review gate to feature work
   - أي feature جديدة تحدد قبل التنفيذ:
     - dominant task
     - primary user
     - detail surface owner
     - disclosure level

2. Add page complexity threshold
   - إذا تجاوزت الصفحة حدودًا معينة في:
     - section count
     - action count
     - metadata layers
   تُراجع قبل التوسع عليها

3. Add object duplication review
   - أي object يظهر في شاشة جديدة يجب تقييم أثر ذلك على mental model

### Exit Criteria

- feature additions تمر عبر gate UX بنيوي
- لا تتمدد screens critical paths بلا مراجعة

## Recommended Execution Order

### Phase A

1. Track 1
2. Track 2

### Phase B

3. Track 3
4. Track 4

### Phase C

5. Track 5

## Recommended Screen Priority

Order of attention:
1. `Approvals`
2. `Intake`
3. `Requests`
4. `Operations`
5. `Dispatch`
6. `Administration`

Reason:
- `Approvals` هي أعلى شاشة قرارية وأكثرها كثافة
- `Intake` هي أعلى شاشة تحميلًا إدراكيًا على المشغل
- `Requests` هي بداية الرحلة الذاتية لصاحب الطلب

## Success Definition

The UX correction phase is considered successful when:
- المستخدم يستطيع فهم "ما المطلوب الآن" دون قراءة كل شيء في الشاشة
- كل شاشة تملك غرضًا تشغيليًا مهيمنًا واضحًا
- الحوكمة تظل موجودة، لكن لا تزاحم الفعل الأساسي
- ledger وdossier لا يختفيان، لكن لا يسيطران على كل surface تنفيذية
- إضافة policy أو metadata جديدة لا تزيد الصفحة تعقيدًا تلقائيًا

## 2026-03-13 Closure Note

Tracks `1-4` are now materially addressed in the implemented frontend
baseline.

Track `5` remains active as an ongoing guardrail, not as an unfinished big-bang
reconstruction step.

The next UX move should therefore be:

- validate the reconstructed surfaces with real role scenarios
- capture friction that still survives the new composition model
- introduce only targeted corrections from that evidence

## Final Strategic Note

المشكلة الحالية في `BG` ليست ضعفًا في الأعمال أو الحوكمة.

المشكلة هي أن النظام بدأ يحمّل كل واجهة تشغيلية أكثر مما يجب من:
- الشرح
- التتبع
- الإثبات
- والسياق

إذا لم يُصحح ذلك الآن، فسيتحول النمو القادم من "زيادة قدرات" إلى "زيادة ثقل"، وسيبدأ المستخدم بالشعور أن النظام يطلب منه فهم النظام كله لأداء مهمة واحدة.
