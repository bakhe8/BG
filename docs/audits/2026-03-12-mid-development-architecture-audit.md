# Mid-Development Architecture Audit

Date: 2026-03-12

Scope:
- Codebase snapshot in `src/` and `tests/`
- `dotnet build BG.sln --no-restore`
- `dotnet test tests/BG.UnitTests/BG.UnitTests.csproj`

Verification baseline:
- Build status: pass
- Test status: pass (`91/91`)

## Executive Verdict

الاتجاه المعماري العام صحيح، لكن النظام ما زال في مرحلة "foundation with growing business realism", وليس في مرحلة "governed platform".

الحكم الحالي:
- Layering and domain direction: strong
- Workflow and operational modeling: strong but still fragile
- Security and governance: not yet acceptable
- Configurability: promising but under-constrained
- Operability and production readiness: partial

التقدير العام لهذه المرحلة: `6.5/10`

قرار معماري مقترح:
- لا أوصي بتوسيع الميزات الأفقية الآن قبل إغلاق 3 blockers:
  1. هوية وصلاحيات حقيقية وليست قابلة للانتحال.
  2. حماية صلاحيات الـ API والمسارات الإدارية بشكل موحد.
  3. قيود سلامة على تعريفات الـ workflow حتى لا يستطيع النظام إدخال نفسه في حالة غير قابلة للتنفيذ.

## What Is Architecturally Working

- الطبقات الأساسية واضحة ومتماسكة:
  - `BG.Domain`
  - `BG.Application`
  - `BG.Infrastructure`
  - `BG.Web`
  - `BG.Integrations`
- aggregate `Guarantee` أصبح مركز الحقيقة التشغيلية فعلا، وليس مجرد جدول بيانات:
  - `src/BG.Domain/Guarantees/Guarantee.cs`
- workflow approvals أصبحت modeled as data/runtime objects بدل كونها if/else متناثر:
  - `src/BG.Domain/Workflow/RequestApprovalProcess.cs`
  - `src/BG.Domain/Workflow/RequestWorkflowDefinition.cs`
- يوجد ledger فعلي ومترابط مع الطلبات والمراسلات والمستندات:
  - `src/BG.Domain/Guarantees/GuaranteeEvent.cs`
- فصل البيئات التشغيلية حسب الدور تطور بشكل جيد على مستوى الـ UX:
  - `src/BG.Web/UI/WorkspaceShellService.cs`
  - `src/BG.Web/UI/WorkspaceAccessMiddleware.cs`
- workflow templates أصبحت قابلة للإدارة من قاعدة البيانات بدل hard-coded runtime only:
  - `src/BG.Application/Services/WorkflowTemplateService.cs`
  - `src/BG.Application/Services/WorkflowAdministrationService.cs`

## Findings

### Critical 1: The current "sign-in" model is user selection, not authentication

Evidence:
- `src/BG.Web/Pages/SignIn.cshtml.cs:48-79`
- `src/BG.Application/Services/UserAccessProfileService.cs:16-27`
- `src/BG.Domain/Identity/User.cs:3-52`

Why this matters:
- صفحة الدخول تعرض المستخدمين النشطين وتسمح بتسجيل الدخول بمجرد اختيار `UserId`.
- نموذج `User` لا يحتوي أصلًا على password hash أو credential primitive.
- النتيجة: أي شخص يصل إلى الصفحة يستطيع انتحال أي مستخدم محلي نشط.

Architectural impact:
- كل ledger وapproval وactor isolation تصبح قابلة للطعن تشغيليًا.
- لا يمكن اعتبار النظام "system of record" بينما الهوية نفسها غير مثبتة.

Required correction:
- إدخال authentication boundary حقيقية:
  - password-based local auth مؤقتًا
  - أو external identity integration لاحقًا
- حتى لو بقي auth محليًا، يجب أن يصبح هناك `credential store`, `password hashing`, وaccount lifecycle.

### Critical 2: Admin API endpoints are not protected by authorization

Evidence:
- `src/BG.Web/Controllers/IdentityAdministrationController.cs:11-74`
- `src/BG.Web/UI/WorkspaceAccessMiddleware.cs:14-20`
- `src/BG.Web/UI/WorkspaceAccessMiddleware.cs:42-51`
- `src/BG.Web/Program.cs:70-83`

Why this matters:
- `api/admin/identity` exposes:
  - list users
  - create users
  - list roles
  - create roles
  - list permissions
- لا توجد `[Authorize]` attributes.
- الـ middleware الحالي يحمي page-paths المعروفة، وليس API controllers.

Architectural impact:
- واجهة الإدارة مؤمنة جزئيًا بصريًا، لكن طبقة الـ API الإدارية نفسها مفتوحة.
- هذا انقسام خطير بين "visible security" و"actual security".

Required correction:
- اعتماد policy-based authorization على controllers والصفحات.
- نقل permission enforcement إلى authorization policies أو endpoint conventions بدل الاعتماد على path middleware فقط.

### High 3: Workflow definitions can become invalid at runtime

Evidence:
- `src/BG.Domain/Workflow/RequestWorkflowDefinition.cs:57-92`
- `src/BG.Domain/Workflow/RequestWorkflowStageDefinition.cs:61`
- `src/BG.Application/Services/WorkflowAdministrationService.cs:67-146`
- `src/BG.Web/Pages/Administration/Workflow.cshtml.cs:48-88`
- `src/BG.Infrastructure/Persistence/DatabaseInitializationExtensions.cs:39-67`

Why this matters:
- stage `RoleId` is nullable.
- admin update flow can set `roleId = null`.
- admin flow can remove stages without guardrails.
- bootstrap seeds workflow definitions with `roleId: null`.

Architectural impact:
- النظام قد يحمل workflow active لكنه غير قابل للتنفيذ.
- approval queue قد تصبح فارغة ليس لأن العمل انتهى، بل لأن التعريف صار invalid.
- failure mode هنا "silent operational dead-end", وهو أخطر من crash واضح.

Required correction:
- validator صريح على workflow definition:
  - must have at least one stage
  - active stages must have assigned roles
  - optionally prevent deleting the last stage
- lifecycle واضح:
  - `DraftDefinition`
  - `ActiveDefinition`
  - `ArchivedDefinition`

### High 4: The intake extraction architecture is still a placeholder, not the target pipeline

Evidence:
- `ARCHITECTURE.md` extraction section
- `src/BG.Application/Services/LocalIntakeExtractionEngine.cs:14-69`

Why this matters:
- الوثيقة المعمارية تعتمد text-first/PDF-first/OCR-fallback pipeline.
- التنفيذ الحالي يعتمد:
  - filename heuristics
  - sample fields
  - generated dates and amounts مثل `950000`

Architectural impact:
- هذه الطبقة ما زالت scaffolding، وليست تنفيذًا فعليًا للمسار الأساسي الذي بُنيت عليه أعمال الإدخال.
- أي توسع كبير فوقها الآن سيضاعف إعادة العمل لاحقًا.

Required correction:
- تثبيت extraction boundary قبل توسيع intake features:
  - document classifier
  - direct PDF extraction adapter
  - OCR adapter
  - confidence scoring contract
  - field provenance contract

### Medium 5: Source of truth is split across static catalogs, database rows, and resource-key mapping functions

Evidence:
- `src/BG.Application/Intake/IntakeScenarioCatalog.cs:11-124`
- `src/BG.Application/Operations/RequestWorkflowTemplateCatalog.cs:5-96`
- `src/BG.Infrastructure/Persistence/DatabaseInitializationExtensions.cs:39-67`
- `src/BG.Application/Services/WorkflowTemplateService.cs:18-57`
- `src/BG.Application/Services/IntakeSubmissionService.cs:273-420`
- `src/BG.Application/Services/OperationsReviewQueueService.cs:216-283`
- `src/BG.Web/Pages/Intake/Workspace.cshtml.cs:51-59`

Why this matters:
- intake scenarios موجودة ككاتالوج ثابت، ثم يعاد تفسيرها في services والweb.
- workflow templates موجودة ككاتالوج ثابت للseed، ثم تصبح rows في database.
- enum-to-resource mapping موزع في عدة خدمات بدل مركزية واحدة.

Architectural impact:
- التغيير الواحد يمر عبر عدة طبقات متوازية.
- خطر drift مرتفع جدًا مع نمو الأنواع والحالات.

Required correction:
- تحديد source of truth واحد لكل محور:
  - intake scenario metadata: typed catalog or persisted config
  - workflow template runtime: database only after seeding
  - presentation key mapping: centralized projection layer

### Medium 6: Query shape is still "load the workspace" rather than "page the workflow"

Evidence:
- `src/BG.Infrastructure/Persistence/Repositories/RequestWorkspaceRepository.cs:48-62`
- `src/BG.Infrastructure/Persistence/Repositories/ApprovalQueueRepository.cs:109-140`
- `src/BG.Infrastructure/Persistence/Repositories/OperationsReviewRepository.cs:48-59`
- `src/BG.Infrastructure/Persistence/Repositories/DispatchWorkspaceRepository.cs:48-55`

Why this matters:
- القوائم الأساسية تحمل كل العناصر مع eager loading واسع.
- لا توجد pagination أو query slicing أو separate read models.

Architectural impact:
- سيعمل الآن، لكنه سيبدأ بالتدهور مبكرًا بمجرد نمو المستندات والledger والموافقات.
- الأداء سيصبح side-effect of entity graph shape, لا قرارًا معماريًا مقصودًا.

Required correction:
- إدخال read-model queries موجهة للـ UI.
- pagination/slicing على queues.
- فصل aggregate-loading عن dashboard/workspace listing.

## Alignment Against The Baseline

Aligned:
- ASP.NET Core 8
- layered separation
- PostgreSQL
- Razor Pages for internal UI
- configurable workflow direction
- local users for current phase
- bilingual UI base
- document-driven operational model

Partially aligned:
- local authorization exists, but not yet enforced safely end-to-end
- workflow configurability exists, but lacks integrity constraints
- intake workflow exists, but extraction engine is still stubbed

Not yet aligned enough:
- audit-grade identity
- audit-grade API protection
- production-grade document intelligence pipeline

## Current Maturity Snapshot

- Business flow realism: `8/10`
- Domain modeling: `7.5/10`
- Security architecture: `3/10`
- Configurability: `6/10`
- Operational readiness: `5/10`
- Scalability posture: `5/10`

## Recommended Next Sequence

1. Security hardening sprint
   - real local credentials
   - authorization policies
   - secure admin API
2. Workflow integrity sprint
   - active-definition validation
   - no roleless active stages
   - no empty workflows
3. Canonical metadata sprint
   - typed scenario keys
   - centralized enum/resource projection
   - single workflow source of truth
4. Extraction architecture sprint
   - replace stub engine contractually before feature growth
5. Read-model and scaling sprint
   - queue pagination
   - UI projections
   - query shaping

## Bottom Line

المعمارية لم تنحرف بعد، لكن هناك خطر انحراف حقيقي إذا استمر البناء فوق:
- هوية قابلة للانتحال
- API إداري غير محمي
- workflow قابل لأن يصبح غير صالح
- ومصادر حقيقة موزعة

إذا أُغلقت هذه الأربع مبكرًا، فالاتجاه الحالي قابل لأن يتحول إلى منصة داخلية قوية بدل أن يتراكم كهيكل صحيح شكليًا فقط.
