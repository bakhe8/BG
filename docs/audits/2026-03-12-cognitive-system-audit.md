# Cognitive System Audit

Date: 2026-03-12

Purpose:
- تقييم مدى وضوح النظام ذهنيًا للمطورين والمشغلين
- قياس سهولة تفسير الحالات، المسؤوليات، ومصادر الحقيقة
- اكتشاف أماكن الانحراف قبل أن تتحول إلى complexity trap

## Executive Verdict

النظام أصبح يحمل mental model صالحًا على مستوى الأعمال:
- `Intake -> Operations -> Requests -> Approvals -> Dispatch -> Bank Response`

لكن mental model الكودي ما زال أقل نضجًا من mental model الأعمال.

الخلاصة:
- business cognition: good
- developer cognition: medium
- operational audit cognition: medium
- system-of-record cognition: medium-low

التقدير العام: `6/10`

## What Is Cognitively Strong

### 1. Role-oriented workspaces are clear

Evidence:
- `src/BG.Web/UI/WorkspaceShellService.cs`
- `src/BG.Web/UI/WorkspaceAccessMiddleware.cs`
- pages under:
  - `src/BG.Web/Pages/Intake`
  - `src/BG.Web/Pages/Operations`
  - `src/BG.Web/Pages/Requests`
  - `src/BG.Web/Pages/Approvals`
  - `src/BG.Web/Pages/Dispatch`

Why this is good:
- كل دور يرى مساحة عمل مرتبطة بمهمته.
- هذا يقلل التشتيت ويقرب النظام من نموذج "purpose-built workstations".

### 2. The domain speaks the business language

Evidence:
- `src/BG.Domain/Guarantees/Guarantee.cs`
- `src/BG.Domain/Guarantees/GuaranteeRequest.cs`
- `src/BG.Domain/Workflow/RequestApprovalProcess.cs`
- `src/BG.Domain/Workflow/ApprovalDelegation.cs`

Why this is good:
- المصطلحات الأساسية صحيحة:
  - guarantee
  - request
  - correspondence
  - approval process
  - delegation
- هذا يجعل تطور الأعمال قابلًا للفهم دون ترجمة داخلية معقدة.

### 3. Ledger direction improves traceability thinking

Evidence:
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs`

Why this is good:
- وجود ledger ككيان أولي يفرض على النظام التفكير في "what happened" وليس فقط "what is the current state".

## Cognitive Findings

### Finding 1: Authentication semantics are misleading

Evidence:
- `src/BG.Web/Pages/SignIn.cshtml.cs:48-79`
- `src/BG.Domain/Identity/User.cs:3-52`

Problem:
- النظام يسمي العملية "Sign in".
- لكن فعليًا هي "Select a local user session".

Why this is cognitively expensive:
- المطور يظن أن auth boundary موجودة بينما الموجود هو session identity picker.
- المشرف قد يظن أن اسم المنفذ في ledger يعكس هوية مثبتة، بينما هذا غير صحيح بعد.

Recommendation:
- إمّا تنفيذ auth حقيقي، أو إعادة تسمية التجربة مؤقتًا بوضوح حتى لا يختلط المفهوم.

### Finding 2: Intake scenario understanding is fragmented across layers

Evidence:
- `src/BG.Application/Intake/IntakeScenarioCatalog.cs:11-124`
- `src/BG.Application/Services/IntakeSubmissionService.cs:273-420`
- `src/BG.Application/Services/OperationsReviewQueueService.cs:216-283`
- `src/BG.Web/Pages/Intake/Workspace.cshtml.cs:51-59`

Problem:
- مفهوم `scenario` الواحد موزع بين:
  - catalog
  - submission logic
  - operations compatibility logic
  - page booleans in web

Why this is cognitively expensive:
- لا يوجد مكان واحد يجيب عن سؤال:
  - ما هو هذا السيناريو؟
  - ما حقوله؟
  - ما أثره؟
  - إلى أين يذهب؟

Recommendation:
- تحويل scenario إلى typed object/identifier مع behavior registry موحد.

### Finding 3: Workflow truth is split between template catalog, database, and admin mutations

Evidence:
- `src/BG.Application/Operations/RequestWorkflowTemplateCatalog.cs:5-96`
- `src/BG.Infrastructure/Persistence/DatabaseInitializationExtensions.cs:39-67`
- `src/BG.Application/Services/WorkflowTemplateService.cs:18-57`
- `src/BG.Application/Services/WorkflowAdministrationService.cs:67-146`

Problem:
- يوجد workflow truth "قبل seed"
- ثم workflow truth "بعد seed"
- ثم workflow truth "بعد تعديل الإدارة"

Why this is cognitively expensive:
- من الصعب الإجابة بسرعة على:
  - ما هو المسار الفعلي الآن؟
  - هل هذا ناتج من catalog أم من DB أم من تعديل إداري؟

Recommendation:
- بعد bootstrap، يجب أن تصبح database هي source of truth الوحيدة، مع validator وصلاحية واضحة للنسخة الفعالة.

### Finding 4: The same enum/state language is retranslated repeatedly

Evidence:
- `src/BG.Application/Services/RequestWorkspaceService.cs:398-434`
- `src/BG.Application/Services/ApprovalQueueService.cs:282-352`
- `src/BG.Application/Services/DispatchWorkspaceService.cs:126-157`
- `src/BG.Application/Services/OperationsReviewMatchingService.cs:198-220`

Problem:
- كل خدمة تقريبًا تعيد تعريف:
  - request type -> resource key
  - status -> resource key
  - category -> resource key

Why this is cognitively expensive:
- المعنى ليس مركزيًا.
- المطور لا يعرف أي mapping هو المرجع.

Recommendation:
- introduction of a shared presentation projection layer.

### Finding 5: The ledger is structurally good but semantically mixed

Evidence:
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:98`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:125`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:145-146`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:179`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:279`

Problem:
- domain stores human-readable English summaries.
- هذا يخلط:
  - event semantics
  - rendering
  - operator-facing narrative

Why this is cognitively expensive:
- يصعب بناء narrative audit bilingual later.
- يصعب استخراج reports دقيقة consistent across languages.

Recommendation:
- اجعل الحدث structured بالكامل.
- وابن localized summary في read model/UI.

### Finding 6: Service hotspots are becoming cognitive choke points

Evidence:
- `src/BG.Application/Services/IntakeSubmissionService.cs` (`466` lines)
- `src/BG.Application/Services/RequestWorkspaceService.cs` (`448` lines)
- `src/BG.Application/Services/ApprovalQueueService.cs` (`355` lines)
- `src/BG.Application/Services/OperationsReviewQueueService.cs` (`351` lines)

Problem:
- هذه الملفات أصبحت نقاط تجميع للمعرفة التشغيلية.

Why this is cognitively expensive:
- أي تعديل بسيط يحتاج إعادة تحميل سياق كبير.
- review quality ستبدأ بالانخفاض مع كل feature جديدة.

Recommendation:
- split by use case responsibility, not just by namespace.

## Cognitive Drift Risks If Development Continues As-Is

1. "Scenario drift"
   - كل layer يفسر scenario بطريقته.
2. "Workflow drift"
   - تعريفات المسارات قد تصبح valid in storage but invalid in operation.
3. "Security narrative drift"
   - users will believe the audit trail is stronger than it really is.
4. "Presentation drift"
   - نفس الحالة قد تظهر بمفاتيح أو تفسيرات مختلفة بين المساحات.

## Cognitive Reduction Plan

### Phase A: Clarify identity truth

- implement real auth
- unify actor and user semantics
- make audit trail claims trustworthy

### Phase B: Canonicalize scenario and workflow metadata

- typed scenario keys
- single workflow truth after seed
- invariant validator for active definitions

### Phase C: Separate semantics from rendering

- event payloads stay in domain
- labels and summaries move to projection layer

### Phase D: Break service hotspots into use-case modules

- request creation
- request submission
- approval decisioning
- dispatch recording
- bank response application

## Bottom Line

من ناحية business cognition، أنتم على الطريق الصحيح.

من ناحية developer cognition، هناك بداية واضحة لتراكم complexity غير المرئية:
- سلاسل نصية موزعة
- مصادر حقيقة متعددة
- وخدمات أصبحت تحمل معرفة أكبر من اللازم

إذا عولجت هذه النقاط الآن، فالنظام سيبقى "easy to extend".
إذا لم تُعالج، فسيتحول إلى نظام صحيح وظيفيًا لكنه صعب الفهم والصيانة، وهذا هو أخطر شكل من الانحراف المبكر.
