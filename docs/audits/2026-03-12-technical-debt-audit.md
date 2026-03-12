# Technical Debt Audit

Date: 2026-03-12

Build and test baseline:
- `dotnet build BG.sln --no-restore`: pass
- `dotnet test tests/BG.UnitTests/BG.UnitTests.csproj`: pass (`91/91`)

Repo snapshot indicators:
- Source files under `src/`: `245`
- Approximate app code lines (`.cs` + `.cshtml`, excluding generated designers): `13,512`
- Test files under `tests/BG.UnitTests`: `32`
- Approximate test code lines: `4,298`
- EF Core migrations so far: `12`

## Executive Summary

الدين التقني الحالي ليس "فوضى"، لكنه بدأ يتبلور في أربع مناطق واضحة:
- Security debt
- Source-of-truth debt
- Service-size debt
- Query-shape debt

إذا استمر التوسع الوظيفي دون معالجة هذه المناطق، فالفائدة قصيرة المدى ستتحول إلى cost multiplier في كل feature جديدة.

## Debt Register

### P0: Identity debt

Evidence:
- `src/BG.Web/Pages/SignIn.cshtml.cs:48-79`
- `src/BG.Domain/Identity/User.cs:3-52`

Debt:
- لا يوجد credential model فعلي.
- sign-in الحالي مبني على اختيار مستخدم، لا إثبات هوية.

Interest:
- أي تدقيق لاحق للـ ledger أو approvals يصبح ضعيفًا.
- سيجبرك هذا على إعادة بناء auth model تحت ضغط لاحقًا.

Paydown action:
- إضافة password hash + sign-in service + lockout/session rules.

### P0: Authorization debt

Evidence:
- `src/BG.Web/Controllers/IdentityAdministrationController.cs:11-74`
- `src/BG.Web/UI/WorkspaceAccessMiddleware.cs:14-20`
- `src/BG.Web/UI/WorkspaceAccessMiddleware.cs:42-51`

Debt:
- authorization محصور في custom middleware path-based.
- admin APIs غير مغطاة بهذا المسار.

Interest:
- توسع الـ API سيخلق مسارات مفتوحة جديدة بسهولة.
- الحماية ستظل معتمدة على معرفة المطور بأسماء المسارات، لا على policy system.

Paydown action:
- policy-based authorization
- endpoint-level enforcement
- remove implicit trust in path prefixing

### P1: Workflow integrity debt

Evidence:
- `src/BG.Domain/Workflow/RequestWorkflowDefinition.cs:57-92`
- `src/BG.Domain/Workflow/RequestWorkflowStageDefinition.cs:61`
- `src/BG.Application/Services/WorkflowAdministrationService.cs:67-146`
- `src/BG.Infrastructure/Persistence/DatabaseInitializationExtensions.cs:39-67`

Debt:
- يمكن إنشاء أو تعديل workflow active بدون role صالح.
- يمكن إزالة كل المراحل.
- seed الحالي يزرع stages بدون roles.

Interest:
- توقف تشغيلي silent.
- debugging صعب لأن الخطأ يظهر كغياب عناصر queue لا كفشل صريح.

Paydown action:
- active workflow validator
- admin-side validation messages
- startup integrity check

### P1: Stringly-typed scenario debt

Evidence:
- `src/BG.Application/Intake/IntakeScenarioCatalog.cs:11-124`
- `src/BG.Application/Services/IntakeSubmissionService.cs:273-420`
- `src/BG.Application/Services/OperationsReviewQueueService.cs:216-283`
- `src/BG.Web/Pages/Intake/Workspace.cshtml.cs:51-59`

Debt:
- نفس scenario keys يعاد استخدامها كسلاسل نصية في application, web, and operations logic.

Interest:
- أي rename أو إضافة scenario جديدة سيحتاج touching عدة ملفات.
- regression risk مرتفع حتى في التعديلات الصغيرة.

Paydown action:
- introduce typed `IntakeScenarioKey`
- central scenario behavior registry

### P1: Projection and mapping duplication debt

Evidence:
- `src/BG.Application/Services/RequestWorkspaceService.cs:398-434`
- `src/BG.Application/Services/ApprovalQueueService.cs:282-352`
- `src/BG.Application/Services/DispatchWorkspaceService.cs:126-157`
- `src/BG.Application/Services/OperationsReviewMatchingService.cs:198-220`

Debt:
- mapping من enum/status/category إلى resource keys مكرر في عدة خدمات.

Interest:
- increases maintenance cost
- creates subtle UI drift between modules

Paydown action:
- create centralized presentation mapping layer
- keep domain enums free of UI duplication

### P1: Service-size debt

Evidence:
- `src/BG.Application/Services/IntakeSubmissionService.cs` (`466` lines)
- `src/BG.Application/Services/RequestWorkspaceService.cs` (`448` lines)
- `src/BG.Application/Services/ApprovalQueueService.cs` (`355` lines)
- `src/BG.Application/Services/OperationsReviewQueueService.cs` (`351` lines)

Debt:
- services تجمع validation + orchestration + parsing + mapping + persistence decisions.

Interest:
- unit testing stays possible, but feature evolution becomes slower and riskier.
- onboarding cost rises sharply.

Paydown action:
- extract:
  - validators
  - mappers/projections
  - scenario handlers
  - workflow integrity policies

### P2: Read-model and pagination debt

Evidence:
- `src/BG.Infrastructure/Persistence/Repositories/RequestWorkspaceRepository.cs:48-62`
- `src/BG.Infrastructure/Persistence/Repositories/ApprovalQueueRepository.cs:109-140`
- `src/BG.Infrastructure/Persistence/Repositories/OperationsReviewRepository.cs:48-59`
- `src/BG.Infrastructure/Persistence/Repositories/DispatchWorkspaceRepository.cs:48-55`

Debt:
- workspace lists load full graphs with no paging.

Interest:
- performance degradation will arrive as data grows, not as code changes.
- UI response time becomes DB-shape dependent.

Paydown action:
- dedicated read queries
- pagination
- summary DTO projections from SQL

### P2: Ledger localization debt

Evidence:
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:98`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:125`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:145-146`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:179`
- `src/BG.Domain/Guarantees/GuaranteeEvent.cs:279`

Debt:
- ledger stores English human-readable summaries inside domain code.

Interest:
- bilingual system loses consistency in audit displays.
- structured reporting/search on event meaning becomes harder.

Paydown action:
- keep event type + structured payload as the source of truth
- generate localized summaries in projection/UI layer

### P2: Test pyramid debt

Evidence:
- test source files: `32`
- test source lines: `4,298`
- search across tests found no `WebApplicationFactory`, `TestServer`, `Playwright`, or hosted end-to-end coverage

Debt:
- الاختبارات الحالية جيدة على مستوى unit tests، لكنها لا تغطي:
  - auth pipeline
  - middleware enforcement
  - EF query behavior against real database
  - full request-to-bank-response flow in hosted runtime

Interest:
- regressions ستظهر متأخرًا عند دمج الطبقات.

Paydown action:
- add:
  - minimal integration test project
  - auth and admin API protection tests
  - EF-backed workflow/runtime tests

## Debt Trend Assessment

Healthy debt:
- workflow configurability
- ledger depth
- role-specific workspaces

Risky debt:
- auth model
- admin protection
- duplicated scenario logic
- large service orchestration files

Dangerous debt:
- none in terms of code chaos
- but the security/auth debt is dangerous in terms of trustworthiness

## Recommended Paydown Order

1. Identity and authorization
2. Workflow validity constraints
3. Scenario typing and centralized mapping
4. Integration tests
5. Read-model/pagination optimization
6. Ledger rendering localization

## Definition of "Debt Under Control" For The Next Phase

قبل فتح مرحلة feature expansion التالية، أوصي باعتبار الدين "تحت السيطرة" فقط إذا تحقق الآتي:

- no user can sign in by selecting any account without credentials
- admin APIs are protected by explicit authorization policy
- active workflows cannot be saved in invalid form
- intake scenarios are typed, not duplicated raw strings across layers
- at least one hosted integration test exists for:
  - sign-in
  - approval decision
  - dispatch recording
  - bank response application

## Bottom Line

الدين التقني ما زال قابلًا للسداد بسهولة نسبيًا، لكن بعد هذه المرحلة مباشرة سيبدأ بالتحول من "manageable" إلى "structural" إذا لم يتم إيقاف التوسع الوظيفي مؤقتًا لإغلاق الـ P0 والـ P1.
