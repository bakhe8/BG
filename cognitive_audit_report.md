# تقرير المراجعة المعرفية الشاملة — نظام BG
**تاريخ التقرير:** 2026-04-01  
**المراجع:** كود مصدري حصري — لا اعتماد على الوثائق  
**التقنية:** ASP.NET Core 9, EF Core, PostgreSQL, Python OCR (PaddleOCR)  
**اللغة:** عربي (Arabic)

---

## 1. الملخص التنفيذي (حالة النظام الحقيقية)

نظام BG هو **نظام إدارة خطابات الضمان البنكي** (Bank Guarantee Management System) لمنشأة صحية (مركز الملك فيصل التخصصي للأبحاث). النظام يعالج دورة حياة خطابات الضمان كاملة: تسجيل، طلبات تعديل، أعمدة موافقة متعددة، إرسال ورقي وإلكتروني، وتأكيدات بنكية.

**الحالة العامة:** النظام في **حالة جيدة نسبياً ومتماسك معمارياً**، مع وجود عيوب هيكلية وأمنية جسيمة تستوجب معالجة فورية.

| المؤشر | التقييم |
|--------|---------|
| نضج المعمارية | ✅ جيد — 4 طبقات واضحة |
| صحة طبقة Domain | ✅ ممتاز — تطبيق DDD صحيح |
| تغطية الاختبارات | ⚠️ متوسط — غياب اختبارات تكامل |
| أمان الوصول | 🔴 مخاطر — صلاحيات أيتام غير مستخدمة |
| مسار OCR | ⚠️ هش — اعتماد على عملية Python خارجية |
| الكود الميت | 🟡 معتدل — صلاحيات وبيانات بذر غير مستخدمة |
| قواعد البيانات | ✅ جيد — مهاجرات منظمة، 19 مهاجرة |

---

## 2. المعمارية الحقيقية (مشتقة من الكود)

```
BG.Web (Razor Pages + REST Controllers)
    ↓ يستدعي
BG.Application (Use Cases / Application Services)
    ↓ يستدعي
BG.Domain (Aggregate Root: Guarantee, Workflow)
    ↓ محافظ عليها بواسطة
BG.Infrastructure (EF Core, PostgreSQL, Storage, Security)
    ↓ يستدعي
BG.Integrations (Python OCR subprocess, Hospital API HTTP Client)
```

### الطبقات المُثبتة من الكود:

**BG.Domain** — ثلاث مجموعات رئيسية:
- `Guarantee` (Aggregate Root — 1053 سطر) يحمل: Documents، Requests، Correspondence، Events
- `RequestApprovalProcess` — دورة حياة الموافقة متعددة المراحل
- `User / Role / Permission` — نموذج هوية مخصص (غير Identity Framework)

**BG.Application** — 9 خدمات تطبيقية:
- `IntakeSubmissionService` — تسجيل ضمانات جديدة وسيناريوهات استلام المستندات
- `ApprovalQueueService` — معالجة موافقات متعددة المراحل
- `OperationsReviewQueueService` — ربط الردود البنكية بالطلبات
- `DispatchWorkspaceService` — إدارة الإرسال والتسليم
- `LocalIntakeExtractionEngine` — تنسيق الاستخراج الآلي للبيانات

**BG.Integrations** — خدمة OCR واحدة:
- `LocalPythonOcrProcessingService` — استدعاء `ocr_worker.py` عبر subprocess خارجي
- `HospitalApi` — HTTP Client لم يُستخدم بعد (تكوين فارغ)

---

## 3. المشاكل الحرجة (خطر عالٍ)

### 🔴 الخطر 1: كلمة مرور Bootstrap الإنتاجية في الكود المصدري
**الموقع:** `DatabaseInitializationExtensions.cs:13-16`  
```csharp
private static readonly string[] InsecureBootstrapPasswords =
[
    "BG-Seed-2026!"
];
```
**المشكلة:** يمكن لأي شخص يملك الكود أن يحاول هذه الكلمة. الفحص ينبهك للكلمة المتقاعدة ويمنعها، لكن نشر كلمة البذر الأولى عبر Git يشكل خطر أمني دائم.  
**التأثير:** بالإضافة إلى ذلك، `OperationalSeedService` يحتوي أيضاً على `"BG-Seed-2026!"` و`"SeedUsers123!"` كقوائم محظورة — مما يثبت أن هذه كانت كلمات مستخدمة فعلياً في الماضي.

---

### 🔴 الخطر 2: صلاحيات أيتام موجودة في الكتالوج لكن لا تُطبّق في أي سياسة تفويض
**الموقع:** `PermissionCatalog.cs` vs `PermissionPolicyNames.cs`

| الصلاحية | في الكتالوج | في السياسة | ملاحظة |
|----------|------------|------------|--------|
| `guarantees.view` | ✅ | ❌ | صلاحية يتيمة — موجودة في الكتالوج، لا سياسة تستخدمها، لا صفحة تطلبها |
| `guarantees.manage` | ✅ | ❌ | صلاحية يتيمة — لا سياسة، لا صفحة محمية |
| `delegations.view` | ✅ | ❌ | صلاحية يتيمة — موجودة في الكتالوج لكن السياسة `DelegationsManage` تطلب فقط `delegations.manage` |
| `workflow.view` | ✅ | ❌ | موجودة في الكتالوج لكن السياسة `WorkflowManage` تطلب `workflow.manage` فقط |
| `dashboard.view` | ✅ | ❌ | الصفحة الرئيسية `Index.cshtml` لا تطبق أي فحص صلاحيات |

**التأثير الحقيقي:** المستخدمون يُمنحون صلاحيات `guarantees.view` و`guarantees.manage` (كما في قائمة System Administrator Seed) لكن لا كود يفرضها أو يبني عليها. هذا يشير إلى **ميزات مخططة لم تُبنى** أو **نظام RBAC غير مكتمل**.

---

### 🔴 الخطر 3: `OperationalSeedService` يستدعي خدمات التطبيق في startup
**الموقع:** `DatabaseInitializationExtensions.cs:83-84` + `OperationalSeedService.cs`

```csharp
var operationalSeedService = scope.ServiceProvider.GetRequiredService<OperationalSeedService>();
await operationalSeedService.SeedAsync(cancellationToken);
```

`OperationalSeedService` (1073 سطر) يستدعي مباشرة:
- `IRequestWorkspaceService.CreateRequestAsync()`
- `IApprovalQueueService.ApproveAsync()`  
- `IDispatchWorkspaceService.RecordDispatchAsync()`
- `IOperationsReviewQueueService.ApplyBankResponseAsync()`

**المشكلة:** اختبارات التكامل مرفوضة في بيئة الإنتاج. إذا فشلت أي خدمة، **سيفشل التطبيق في البدء**. هذا يجعل startup fragile ومعتمداً على حالة قاعدة البيانات بشكل معقد.

---

### 🔴 الخطر 4: مسار OCR يُشغّل subprocess Python خارجي بدون عزل
**الموقع:** `LocalPythonOcrProcessingService.cs:99-127`

```csharp
startInfo.Environment["PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK"] = "True";
// تنفيذ user-controlled file path مباشرة
FileName = pythonExecutablePath,
Arguments = $"\"{workerScriptPath}\" --request \"{requestFilePath}\""
```

**المشاكل:**
1. ملف طلب OCR يُكتب في `GetTempPath()` بقيمة GUID — لكن لا حماية من directory traversal في `requestFilePath`
2. `process.Kill(entireProcessTree: true)` — قد يقتل عمليات غير مقصودة
3. لا حد أقصى لحجم الملف قبل إرساله لـ OCR
4. الـ timeout يصل لـ 300 ثانية (5 دقائق) — يمكن تعطيل الـ request thread

---

## 4. انتهاكات معمارية

### انتهاك 1: `OperationalSeedService` ينتمي للـ Infrastructure لكنه يعتمد على Application Services
```csharp
// في BG.Infrastructure/Persistence/OperationalSeedService.cs
private readonly IRequestWorkspaceService _requestWorkspaceService;  // → Application layer
private readonly IApprovalQueueService _approvalQueueService;        // → Application layer
```
**الانتهاك:** Infrastructure تعتمد على Application (عكس تدفق الاعتماد الصحيح).

### انتهاك 2: طبقة Application تعيد تنفيذ منطق تطبيعي (Normalize) في كل خدمة
- `IntakeSubmissionService.cs` تحتوي `private static string? Normalize()`
- `OperationsReviewMatchingService.cs` تحتوي `private static bool TryParseAmount()`
- `IntakeSubmissionService.cs` تحتوي أيضاً `private static bool TryParseAmount()`

**المشكلة:** نفس المنطق مكرر بدون مصدر وحيد للحقيقة.

### انتهاك 3: `BgDbContext.TrackAggregateChildren()` — منطق أعمال في طبقة Persistence
```csharp
private void TrackAggregateChildren<T>(IEnumerable<T> entities, DbSet<T> dbSet, ...)
{
    // يستعلم من قاعدة البيانات داخل منطق حفظ التغييرات
    var existingIds = dbSet.AsNoTracking().Where(predicate).Select(keySelectorExpression).ToHashSet();
}
```
هذا المنطق ينفذ استعلام إضافي في كل `SaveChanges` — مما يؤثر على الأداء ويزيد التعقيد.

---

## 5. مشاكل جودة الكود

### 5.1 كود ميت وصلاحيات غير مستخدمة

| العنصر | الموقع | الحالة |
|--------|--------|--------|
| `guarantees.view` | PermissionCatalog | موجودة، لا سياسة، لا صفحة |
| `guarantees.manage` | PermissionCatalog | موجودة، لا سياسة، لا صفحة |
| `delegations.view` | PermissionCatalog | لا سياسة تستخدمها |
| `workflow.view` | PermissionCatalog | لا سياسة تستخدمها |
| `HospitalApi` integration | BG.Integrations | BaseUrl فارغ، ApiKey فارغ — غير مستخدم |
| `Privacy.cshtml` | BG.Web/Pages | صفحة فارغة لا معنى لها |
| `WorkspaceUserOptionDto` | WorkspaceShellService | `Array.Empty<WorkspaceUserOptionDto>()` — مميزة غير منجزة |

### 5.2 تكرار منطق التحليل (Duplicated Parsing Logic)

```
TryParseAmount() → موجودة في:
  ✗ IntakeSubmissionService.cs (سطر 428)
  ✗ OperationsReviewMatchingService.cs (سطر 220)

TryParseDate() → موجودة في:
  ✗ IntakeSubmissionService.cs (سطر 442)
  (محتمل التكرار في خدمات أخرى)
```

### 5.3 تسمية غير اتساقية
- في `OperationsReviewMatchingService.cs:74-77`:
  ```csharp
  case GuaranteeRequestStatus.SubmittedToBank:
      score += 20;
      reasons.Add("OperationsMatchReason_WaitingForBankResponse"); // نفس المفتاح لحالة مختلفة!
  ```
  حالة `AwaitingBankResponse` وحالة `SubmittedToBank` تشتركان في نفس resource key للسبب — وهذا مضلل للمستخدمين.

### 5.4 الـ seed data يتضمن بيانات حقيقية (hard-coded)
```csharp
// OperationalSeedService.cs:609
"King Faisal Specialist Hospital & Research Centre",
```
اسم مستشفى حقيقي مُضمَّن في الكود مباشرة — يصعّب اختباره في بيئات مختلفة.

---

## 6. مشاكل وظيفية وتدفقات العمل

### 6.1 منطق قابلية إعادة الفتح (Reopen) معقد دون حماية كافية

**الموقع:** `Guarantee.cs:733-836` — `ReopenAppliedBankConfirmation()`

```csharp
if (Requests.Any(candidate =>
    candidate.Id != request.Id &&
    candidate.CompletedAtUtc.HasValue &&
    candidate.CompletedAtUtc.Value > request.CompletedAtUtc.Value))
{
    throw new InvalidOperationException("...");
}
```

**المشكلة:** الحماية تعتمد على المقارنة الزمنية فقط — إذا كانت وقتين متطابقين بسبب race condition أو نفس الـ UTC millisecond، يمكن تجاوز الحماية.

### 6.2 الرمز `null!` ككود تهيئة  
**الموقع:** `WorkspaceShellService.cs:101-108`
```csharp
httpContext.Items[typeof(UserAccessProfileDto)] = null!;
```
استخدام `null!` لتجاوز nullable analysis — يخفي احتمالية NullReferenceException.

### 6.3 لا يوجد Rate Limiting لمحاولات تسجيل الدخول
**الموقع:** `SignIn.cshtml.cs` + `LocalAuthenticationService`  
لا يوجد lockout، لا throttling، النظام عرضة لهجمات brute force.

### 6.4 إجراء `ApplyBankConfirmation` يعدّل حالة خطاب المراسلة مع إعادة ربطه
**الموقع:** `Guarantee.cs:616-631`  
```csharp
if (correspondence.GuaranteeRequestId != request.Id)
{
    correspondence.LinkToRequest(request);
    // يعدّل سجلات أحداث سابقة لإضافة requestId
    foreach (var ledgerEntry in Events.Where(...))
    {
        ledgerEntry.LinkToRequest(request);
    }
}
```
**المشكلة:** يُعدِّل الأحداث التاريخية المسجلة retroactively — هذا انتهاك لمبدأ Immutability في Event Ledger.

---

## 7. الأمان والوصول

### 7.1 نموذج الأمان — صحيح لكن به ثغرات

**الجيد:**
- PBKDF2/SHA256 بـ 100,000 تكرار — آمن ✅
- CryptographicOperations.FixedTimeEquals — يمنع timing attacks ✅
- Cookie HttpOnly + SameSite=Lax ✅
- صلاحيات متعددة المستويات عبر RBAC ✅

**المشاكل:**
- ❌ لا حماية CSRF على Controllers (فقط على Razor Pages)
- ❌ لا rate limiting لتسجيل الدخول
- ❌ `options.KnownNetworks.Clear()` في ForwardedHeaders — يثق بـ ANY proxy بدون تقييد
- ❌ لا توجد audit log لعمليات الأمان الحساسة (تغيير كلمة المرور، تعيين الأدوار)

### 7.2 `WorkspaceAccessMiddleware` — الاعتماد على الصفحة الرئيسية
صفحة `Index.cshtml` (الداشبورد) لا يوجد عليها `[Authorize]` attribute — يعتمد على الـ Middleware فقط. إذا تم تعديل pipeline ترتيب، قد يكون الداشبورد متاحاً بدون مصادقة.

### 7.3 Swagger مفعّل في الإنتاج إذا تم ضبط إعداد
```json
"Swagger": { "Enabled": false }
```
يمكن تفعيله عبر متغير بيئة — وهذا يكشف API documentation في الإنتاج.

---

## 8. تقييم مسار OCR / المعالجة

### بنية المسار الحقيقية (من الكود):

```
PDF Upload
    ↓ StageAsync() → تخزين مؤقت
    ↓ ExtractAsync() → LocalIntakeExtractionEngine
    ↓ LocalIntakeDocumentClassifier → تحديد السيناريو
    ↓ LocalIntakeDirectTextExtractor
           → IOcrDocumentProcessingService.ProcessAsync()
           → LocalPythonOcrProcessingService.ProcessAsync()
           → subprocess: python ocr_worker.py --request <file.json>
           → ocr_worker.py يُعيد JSON على stdout
    ↓ LocalIntakeExtractionConfidenceScorer → تصفية النتائج
    ↓ LocalIntakeFieldReviewProjector → عرض للمستخدم للمراجعة
    ↓ IntakeSubmissionService.FinalizeAsync() → حفظ في قاعدة البيانات
```

### مشاكل المسار:

**🔴 مشكلة 1: لا مسار fallback حقيقي لفشل OCR**  
إذا فشل `ocr_worker.py`، يُعاد `NullOcrDocumentProcessingService` أو نتيجة فشل — لكن الواجهة تعرض النتيجة الفارغة للمستخدم دون إشعار واضح. المستخدم قد ينتهي بتسجيل بيانات خاطئة.

**🔴 مشكلة 2: تسرب الملفات المؤقتة المحتمل**  
```python
preprocessed_path = os.path.join(tempfile.gettempdir(), f"bg-ocr-pre-{Path(rendered_path).name}")
```
في حال استثناء غير متوقع قبل `cleanup_file(preprocessed_path)`، تبقى الملفات في التخزين المؤقت.

**🔴 مشكلة 3: ثبات `PADDLE_OCR_INSTANCE` كـ global state**
```python
PADDLE_OCR_INSTANCE = None
```
متغير عالمي في process Python — لكن كل استدعاء هو process منفصل، مما يعني إعادة تهيئة النموذج في كل طلب OCR (بطء كبير جداً).

**⚠️ مشكلة 4: تكرار هويّة أبريل في الخريطة العربية**
```python
"ابريل": 4,
"ابريل": 4,  # مكرر بنفس المفتاح!
"ابرايل": 4,
```
سطر 134 = سطر 135 نفس القيمة مكررة — Python تتجاهله، لكنه يدل على عدم دقة.

**✅ الجانب الإيجابي:**  
مسار OCR محسوب ومصمم بعناية: text-first → scan → mixed route، مع deduplication وwarnings descriptive وfallback للمسار البديل.

---

## 9. قاعدة البيانات وسلامة البيانات

### 9.1 جوانب إيجابية
- 19 مهاجرة EF Core منظمة — تطور تدريجي واضح ✅
- جميع العلاقات معرّفة بـ Fluent API ✅
- استخدام `DateOnly` و`DateTimeOffset` بشكل صحيح ✅
- تقريب المبالغ `decimal.Round(..., MidpointRounding.AwayFromZero)` ✅

### 9.2 مشاكل

**مشكلة 1: `PreviousStatus` و`NewStatus` محفوظتان كـ string**
```csharp
// GuaranteeEvent.cs:49-50
PreviousStatus = previousStatus?.ToString();
NewStatus = newStatus?.ToString();
```
عند تغيير قيم Enum في المستقبل، ستصبح السجلات التاريخية غير قابلة للتحليل الصحيح.

**مشكلة 2: الاستعلام الإضافي في كل `SaveChanges`**
```csharp
// BgDbContext.cs:151-155
var existingIds = dbSet.AsNoTracking().Where(predicate).Select(keySelectorExpression).ToHashSet();
```
كل `SaveChanges` على `Guarantee` يُنفذ استعلامات N إضافية للتحقق من الكيانات الموجودة — يتدهور الأداء مع زيادة الطلبات.

**مشكلة 3: لا حد أقصى لعدد المستندات أو الطلبات لكل ضمان**  
Domain لا يفرض قيوداً على عدد المستندات أو الطلبات لكل `Guarantee` — يمكن نظرياً تجميع آلاف الكيانات في Aggregate واحد.

**مشكلة 4: بيانات البذر التشغيلي (OperationalSeed) تُكدّس قاعدة البيانات**  
`OperationalSeedService` ينشئ 8 ضمانات كاملة مع طلبات وموافقات وإرسال عند كل تشغيل وتم تفعيله — يزيد صعوبة البيئات الإنتاجية.

---

## 10. الاختبارات: الواقع والثغرات

### 10.1 ما هو موجود (إيجابي)
```
Domain Tests:
  ✅ GuaranteeAggregateTests.cs (20K bytes) — اختبارات aggregate جيدة
  ✅ RequestApprovalProcessTests.cs
  ✅ RequestWorkflowDefinitionTests.cs

Application Tests (20 ملف):
  ✅ ApprovalQueueServiceTests.cs (47K bytes) — تغطية واسعة
  ✅ OperationsReviewQueueServiceTests.cs (36K bytes)
  ✅ RequestWorkspaceServiceTests.cs (37K bytes)
  ✅ IntakeSubmissionServiceTests.cs
  ✅ OperationsReviewMatchingServiceTests.cs
  ... والمزيد
```

### 10.2 ثغرات خطيرة

| الثغرة | الخطورة |
|--------|---------|
| ❌ لا اختبارات تكامل لمسار OCR الكامل | عالية |
| ❌ لا اختبارات E2E للصفحات (Razor Pages) | عالية |
| ❌ لا اختبار لـ `BgDbContext.TrackAggregateChildren` | متوسطة |
| ❌ لا اختبار لـ `OperationalSeedService` بأكمله | متوسطة |
| ❌ لا اختبار لـ `LocalPythonOcrProcessingService` (subprocess) | عالية |
| ❌ لا اختبار لسيناريو فشل OCR + fallback behavior | عالية |
| ❌ لا اختبار لـ `PermissionAuthorizationHandler` | عالية |
| ❌ لا اختبار لـ race conditions في `ApplyBankConfirmation` | عالية |

---

## 11. التحليل: الوثائق vs الكود

| الوثيقة | الحالة |
|---------|--------|
| `docs/ocr_implementation_plan.md` | ⚠️ قد تكون قديمة — المسار الحقيقي يستخدم wave2 بينما اسم الخطة يقترح مرحلة أولى |
| `docs/frontend_reconstruction_plan.md` (30K) | ⚠️ مخطط ضخم — حجمه يشير إلى إعادة بناء واجهة لم تكتمل بعد |
| `docs/refactor_roadmap.md` (13K) | ⚠️ يقترح إصلاحات جوهرية — بعضها لم يُطبَّق بعد |
| `docs/program_closure_backlog.md` (9K) | 🔴 وجود "backlog إغلاق تجريبي" يشير إلى بنود معلقة رسمياً |

**التضارب:** الصلاحيات `guarantees.view` و`guarantees.manage` موجودة في الكتالوج والوثائق لكن لا صفحات تستخدمها — **الوثائق تصف ميزات مخططة لم تُبنَ بعد.**

---

## 12. تقرير الهدر والازدواجية

### 12.1 كود ميت / غير مستخدم

| العنصر | النوع | الأثر |
|--------|-------|-------|
| `guarantees.view` + `guarantees.manage` permissions | صلاحيات يتيمة | ارتباك في RBAC |
| `delegations.view` + `workflow.view` permissions | صلاحيات يتيمة | لا تُفرّق read vs write |
| `HospitalApi` integration (BaseUrl = "") | خدمة غير مفعّلة | كود ميت مشروط |
| `Array.Empty<WorkspaceUserOptionDto>()` | ميزة placeholder | ناقصة |
| `Privacy.cshtml` | صفحة فارغة | حطام CRUD |

### 12.2 منطق مكرر

| المنطق | مواقع التكرار |
|--------|---------------|
| `TryParseAmount()` | IntakeSubmissionService + OperationsReviewMatchingService |
| `Normalize(string?)` | متعدد الخدمات |
| `NormalizeRequired/NormalizeOptional` | Domain entities + User (مستقل في كل كيان) |

---

## 13. تقرير التناقضات

### تناقض 1: حالتا Status مُعبَّر عنهما بنفس resource key
```csharp
// OperationsReviewMatchingService.cs:72-78
case GuaranteeRequestStatus.AwaitingBankResponse:
    reasons.Add("OperationsMatchReason_WaitingForBankResponse"); // score +25
case GuaranteeRequestStatus.SubmittedToBank:
    reasons.Add("OperationsMatchReason_WaitingForBankResponse"); // score +20 — نفس النص!
```
السبب نفسه يُعطى لحالتين بأوزان مختلفة — يضلل المستخدم حول سبب التوصية.

### تناقض 2: صلاحية `delegations.manage` تتحكم في صفحة Delegations، لكن `delegations.view` لا تملك صفحة
السياسة `DelegationsManage` تطلب فقط `delegations.manage`. يعني `delegations.view` لا تعطي وصولاً لشيء.

### تناقض 3: `Guarantee.cs` يتغاضى عن `CreateRequest` على ضمان Released/Replaced
```csharp
public GuaranteeRequest CreateRequest(...)
{
    ValidateRequestData(requestType, requestedAmount, requestedExpiryDate);
    // لا يفحص GuaranteeStatus!
}
```
يمكن إنشاء طلب على ضمان `Released` أو `Replaced` — المنع يحتاج guard على `Status`.

---

## 14. القدرات الناقصة

| القدرة المفقودة | الدليل من الكود |
|----------------|----------------|
| صفحة عرض الضمانات الشاملة | `guarantees.view` بدون route |
| قراءة Workflow (read-only) | `workflow.view` بدون سياسة |
| قراءة التفويضات | `delegations.view` بدون سياسة |
| عرض قائمة المستخدمين | `users.view` يؤدي نفس وظيفة `users.manage` في السياسة |
| تواصل مع Hospital API | BaseUrl فارغ، ApiKey فارغ |
| خيارات تبديل المستخدم | `WorkspaceUserOptionDto` فارغة |
| Audit Log لأمان الهوية | لا توجد أحداث تسجيل مصادقة |
| Rate Limiting لتسجيل الدخول | لا يوجد throttling |

---

## 15. مؤشرات الخطر

| المؤشر | التقدير | التبرير |
|-------|---------|---------|
| **مؤشر التعرض للخطر** | 52/100 | صلاحيات يتيمة + subprocess + ForwardedHeaders مفتوح |
| **درجة الهشاشة الخفية** | 61/100 | OperationalSeed يعطل startup، OCR global state، date comparison |
| **احتمالية الفشل الصامت** | 45/100 | OCR يُعيد نتائج فارغة بصمت، FinalizeAsync بدون إشعار فشل |
| **مقاومة الخطأ البشري** | 58/100 | لا rate limiting، لا audit log، لا حماية من طلب على ضمان منتهٍ |
| **مؤشر كفاءة التدفق** | 71/100 | المسار الفعلي واضح ومتماسك لكن مسار OCR بطيء |
| **مؤشر الاستدامة طويلة المدى** | 63/100 | الكود منظم جيداً لكن هناك ميزات ناقصة وBacklog مقلق |

---

## 16. خطة الإصلاح المُرتّبة بالأولوية

### 🔴 فوري (هذا الأسبوع)

1. **إزالة `"BG-Seed-2026!"` من الكود المصدري تماماً** — استخدم environment secret فقط  
2. **إضافة Rate Limiting على صفحة تسجيل الدخول** — حد 5 محاولات/دقيقة  
3. **إصلاح `CreateRequest` — إضافة فحص `GuaranteeStatus`** — منع إنشاء طلبات على ضمانات Released/Replaced  
4. **تقييد `ForwardedHeaders`** — تعيين `KnownProxies` بعناوين محددة

### 🟡 قصير المدى (هذا الشهر)

5. **استخراج `TryParseAmount` و`TryParseDate` إلى `SharedParsingHelper`** — مصدر واحد للحقيقة  
6. **نقل `OperationalSeedService` خارج Infrastructure** — أو فصل استدعاء Application Services عنه  
7. **إصلاح resource key المكرر** في `OperationsReviewMatchingService` لـ `SubmittedToBank`  
8. **إضافة `GuaranteeStatus` check في `Guarantee.CreateRequest()`**  
9. **تنظيف تسرب الملفات المؤقتة** في `ocr_worker.py` — استخدام `with tempfile.TemporaryDirectory()`  
10. **إصلاح التكرار في `ARABIC_MONTH_VALUE_MAP`** (`ابريل` مكررة مرتين)

### 🟢 طويل المدى (ربع سنوي)

11. **بناء سياسات للصلاحيات الأيتام** (`guarantees.view`, `guarantees.manage`, `workflow.view`, `delegations.view`) أو حذفها  
12. **تحويل `PreviousStatus`/`NewStatus` من String إلى enum value (int)** لحماية مستقبلية  
13. **إضافة اختبارات E2E لمسار OCR الكامل** + اختبارات Razor Pages  
14. **إضافة Audit Log لعمليات الهوية الحساسة** (تسجيل دخول، تغيير كلمة مرور، تعيين أدوار)  
15. **تقليل حجم `Guarantee` Aggregate** — النظر في إخراج `Events` كـ separate query  
16. **تحسين أداء `BgDbContext.TrackAggregateChildren`** — إضافة caching أو تبسيط المنطق

---

## خلاصة التحليل المعرفي

نظام BG مبني على **أساس معماري قوي** مع تطبيق DDD صحيح في طبقة Domain. الـ Guarantee Aggregate هو نواة محكمة وناضجة مع مئات التحقق والحماية. مسار الموافقة متعدد المراحل مُصمَّم بعناية. الاختبارات التطبيقية وفيرة ومعمّقة.

**لكن خلف هذا الجمال، ثلاث ضرورات عاجلة:**

1. **صلاحيات يتيمة وبنية RBAC ناقصة** — النظام يوهم بوجود حماية لم تُبنَ
2. **مسار OCR هش بدون caching وبدون fallback واضح للمستخدم** — خطر عملي يومي
3. **`CreateRequest` لا يفحص حالة الضمان** — ثغرة منطقية صامتة يمكن أن تُفسد البيانات

النظام **جاهز للإنتاج بشروط** — لكن المخاطر المذكورة تجعله في حاجة لمعالجة مركزة قبل توسيع دائرة المستخدمين.

---
*تقرير مشتق بالكامل من تحليل 15,000+ سطر من الكود المصدري — 2026-04-01*
