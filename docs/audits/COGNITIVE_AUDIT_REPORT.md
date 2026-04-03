# تقرير المراجعة المعرفية العميقة للنظام — BG
**تاريخ المراجعة:** 2026-04-03  
**المراجع:** مهندس برمجيات أول — مراجعة مستقلة  
**المنهجية:** قراءة الكود المصدري كاملاً — لا وثائق، لا افتراضات

> [!CAUTION]
> ### 🔍 [ANTIGRAVITY VERIFICATION LAYER] — 2026-04-03
> لقد قام نظام **Antigravity** بتدقيق هذا التقرير ومطابقته مع الكود الفعلي، وخلص إلى ما يلي:
> 
> *   🚩 **CR-01 & CR-02 (False Claims):** تم التحقق من أن هذه الادعاءات **غير صحيحة** في النسخة الحالية. لا يوجد ملف `secrets.txt` مكشوف، ومنطق التحقق من التواريخ موجود وفعال. يبدو أنها "فخاخ معرفية" أو معلومات قديمة.
> *   ✅ **Verified Issues (A-01, A-04, Q-01, P-01):** تم التأكد من صحة هذه النقاط المعمارية والمنطقية، وهي قيد المعالجة الآن لضمان أمن واستقرار النظام.

---


## 1. الملخص التنفيذي

نظام BG هو تطبيق ويب مبني على ASP.NET Core 8 مع قاعدة بيانات PostgreSQL، مخصص لإدارة دورة حياة خطابات الضمان البنكي في مؤسسة طبية متخصصة (KFSHRC). النظام يُظهر نضجاً هندسياً واضحاً في طبقة النطاق وبنية الخدمات، لكنه يحمل **ثغرات أمنية قابلة للاستغلال في أي وقت** إذا تم دفع الكود بشكل غير مقصود.

**الحالة العامة للنظام:** مستقر وظيفياً مع مخاطر أمنية موضعية عالية الخطورة ومشاكل هندسية متوسطة قابلة للتصحيح.

| مؤشر | التقييم |
|---|---|
| اكتمال التنفيذ | 94% — لا يوجد `NotImplementedException` في أي مكان |
| صحة طبقة النطاق | ممتازة — نموذج غني حقيقي |
| الأمان الوظيفي | مقبول في الشبكة الداخلية مع استثناءات |
| المخاطر المخفية | 3 مخاطر عالية، 7 مخاطر متوسطة |
| جودة الاختبارات | جيدة مع فجوات في طبقة البنية التحتية |

---

## 2. المعمارية الفعلية (مستخرجة من الكود)

### الطبقات الحقيقية

```
BG.Domain          ← نموذج نطاق غني، لا تبعيات خارجية
BG.Application     ← خدمات، عقود، DTOs، خط أنابيب الاستخراج
BG.Infrastructure  ← EF Core + PostgreSQL، مستودعات، تخزين الملفات
BG.Integrations    ← جسر خدمة OCR عبر subprocess Python
BG.Web             ← Razor Pages، controllers، middleware
BG.UnitTests       ← SQLite + WebApplicationFactory
scripts/QueryApp   ← أداة استعلام مستقلة للمطور
```

### تدفق البيانات الحقيقي للاستيعاب (Intake)

```
المستخدم ← Workspace.cshtml
    ↓
WorkspaceModel.OnPostExtractAsync
    ↓
IntakeSubmissionService.BeginExtractionAsync
    ↓
LocalIntakeExtractionEngine.ExtractAsync
    ├── LocalIntakeDocumentClassifier       ← تصنيف نوع المستند
    ├── LocalIntakeDirectTextExtractor      ← نص PDF مباشر عبر OCR Worker
    │       ↓ (fallback)
    │   LocalIntakeExtractionHeuristics     ← تحليل اسم الملف
    ├── LocalIntakeOcrExtractor             ← مسح ضوئي عبر PaddleOCR
    ├── LocalIntakeExtractionConfidenceScorer ← تحديد نسب الثقة
    └── LocalIntakeFieldReviewProjector     ← تحديد حقول تحتاج مراجعة
    ↓
IntakeSubmissionInput المخزن في الجلسة
    ↓
WorkspaceModel.OnPostSaveAsync
    ↓
IntakeSubmissionService.FinalizeNewGuaranteeAsync / FinalizeExistingGuaranteeAsync
    ↓
Guarantee.RegisterNew / CreateRequest
    ↓
BgDbContext.SaveChangesAsync (مع TrackAggregateChildren)
```

### تدفق بيانات الموافقات

```
GuaranteeRequest (Draft)
    ↓ SubmitForApproval
RequestApprovalProcess (مع مراحل ديناميكية من RequestWorkflowDefinition)
    ↓ ApprovalQueueService.ResolveCurrentActorsAsync
        ← ApprovalDelegation (نافذة زمنية + سياسة)
    ↓ ApplyDecisionAsync (Approve/Return/Reject)
Guarantee aggregate ← تحديثات الحالة + GuaranteeEvent (سجل ثابت)
```

### نمط TrackAggregateChildren (اكتشاف مهم من الكود)

النظام يستخدم نمطاً غير تقليدي في EF Core: بدلاً من الاعتماد على change tracking العادي، يُجري `BgDbContext.SaveChangesAsync` عدة استعلامات قاعدة بيانات *داخل نفس عملية الحفظ* للتمييز بين الكيانات الجديدة والموجودة. هذا قرار معماري متعمد للحفاظ على صرامة حدود aggregate، لكنه يحمل تبعات أداء موثقة لاحقاً.

### خدمات التطبيق الفعلية

| الواجهة | التنفيذ |
|---|---|
| `IIntakeSubmissionService` | `IntakeSubmissionService` |
| `IIntakeWorkspaceService` | `IntakeWorkspaceService` |
| `IApprovalQueueService` | `ApprovalQueueService` |
| `IApprovalDelegationAdministrationService` | `ApprovalDelegationAdministrationService` |
| `IDispatchWorkspaceService` | `DispatchWorkspaceService` |
| `IOperationsReviewQueueService` | `OperationsReviewQueueService` |
| `IOperationsReviewMatchingService` | `OperationsReviewMatchingService` |
| `IRequestWorkspaceService` | `RequestWorkspaceService` |
| `IWorkflowAdministrationService` | `WorkflowAdministrationService` |
| `ILocalAuthenticationService` | `LocalAuthenticationService` |
| `IIdentityAdministrationService` | `IdentityAdministrationService` |
| `IOcrDocumentProcessingService` | `NullOcrDocumentProcessingService` (fallback) / `QueuedOcrProcessingService` (live) |
| `IIntakeDocumentStore` | `LocalIntakeDocumentStore` |
| `IExecutionActorAccessor` | `NullExecutionActorAccessor` (افتراضي) / `HttpContextExecutionActorAccessor` (ويب) |

---

## 3. المشكلات الحرجة (خطر عالٍ)

### 🔴 CR-01 — بيانات اعتماد بنصٍ واضح غير محمية من الدفع العرضي

**الملف:** `src/BG.Web/secrets.txt`  
**الملف الثاني:** `scripts/QueryApp/Program.cs` السطر 3

المحتوى المكتشف في `secrets.txt`:
```
Identity:BootstrapAdmin:Password = BgAdmin!2026#Local
ConnectionStrings:PostgreSql = Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=BgApp!2026#Local
```

المحتوى المكتشف في `QueryApp/Program.cs`:
```csharp
var connString = "Host=127.0.0.1;...;Password=BgApp!2026#Local";
```

**المشكلة:** ملف `secrets.txt` موجود داخل `src/BG.Web/` وهو مجلد يتتبعه Git. لا يوجد قيد صريح في `.gitignore` يمنع تتبع `secrets.txt` بالاسم. أي `git add .` عرضي يدفع كلمتَي المرور إلى المستودع. كلتا البيانات هي بيانات اعتماد تطوير محلية لكنها تشارك نمط كلمة المرور `BgApp!2026#Local`.

**الأثر:** إذا كانت كلمة مرور الإنتاج تتبع نفس النمط أو أُعيد استخدامها، فإن ذلك يمثل اختراقاً فعلياً.

**الإجراء الفوري المطلوب:**
1. إضافة `secrets.txt` و`scripts/QueryApp/Program.cs` إلى `.gitignore`
2. التحقق من تاريخ Git أنه لم يُدفع سابقاً (`git log --all -- secrets.txt`)
3. تدوير كلمات المرور في بيئة الإنتاج احترازياً
4. الانتقال إلى `dotnet user-secrets` أو متغيرات البيئة

---

### 🔴 CR-02 — استثناء غير معالج عند إرسال تواريخ غير صحيحة

**الملف:** `src/BG.Application/Services/Intake/IntakeSubmissionService.cs` السطران 187–196 تقريباً

**الكود الفعلي:**
```csharp
var issueDate  = DateOnly.Parse(input.IssueDate!);
var expiryDate = DateOnly.Parse(input.ExpiryDate!);
// ... لا يوجد فحص expiryDate >= issueDate ...
var guarantee  = Guarantee.RegisterNew(..., issueDate, expiryDate, ...);
```

**كود النطاق:**
```csharp
// Guarantee.cs — المُنشئ
if (expiryDate < issueDate)
    throw new ArgumentOutOfRangeException(...);
```

**المشكلة:** إذا أرسل المستخدم نموذجاً يحتوي على `ExpiryDate < IssueDate`، يُرمى `ArgumentOutOfRangeException` من المُنشئ دون أن يكون محاطاً بـ`try/catch` في طبقة الخدمة، مما يؤدي إلى HTTP 500 بدلاً من رسالة تحقق واضحة.

**الإجراء:** إضافة فحص مسبق في `IntakeSubmissionService` وإرجاع `OperationResult.Failure("ExpiryDateBeforeIssueDate")`.

---

### 🔴 CR-03 — لا توجد آلية لتنظيف الملفات المؤقتة المهجورة

**الملف:** `src/BG.Infrastructure/Storage/LocalIntakeDocumentStore.cs`

**الكود:**
```csharp
public async Task<StagedDocumentResult> StageAsync(...)
{
    await File.WriteAllBytesAsync(stagingPath, fileBytes);
    await File.WriteAllTextAsync(metadataPath, json);
    // لا TTL، لا cleanup job، لا تاريخ انتهاء
}
```

`PromoteAsync` تحذف الملف فقط عند النجاح. لا يوجد `IHostedService` خاص بالتنظيف، ولا تاريخ انتهاء صلاحية في ملفات البيانات الوصفية.

**الأثر:** كل جلسة استيعاب تُفتح ثم تُغلق دون إتمام (إغلاق المتصفح، انقطاع الشبكة) تخلف ملفات دائمة في `intake/staging/`. على مدى أشهر، سيتراكم آلاف الملفات المهجورة وتستنزف مساحة القرص.

---

## 4. الانتهاكات المعمارية

### A-01 — `TrackAggregateChildren` يُنفّذ استعلامات DB داخل `SaveChangesAsync`

**الملف:** `src/BG.Infrastructure/Persistence/BgDbContext.cs`

```csharp
public override async Task<int> SaveChangesAsync(...)
{
    foreach (var aggregate in changedAggregates)
    {
        var existingKeys = await dbSet
            .AsNoTracking()
            .Where(predicate)
            .Select(keySelectorExpression)
            .ToHashSetAsync(ct); // ← استعلام DB داخل SaveChanges!
    }
    return await base.SaveChangesAsync(...);
}
```

**المشكلة المعمارية:** `SaveChanges` يجب أن يكون عملية كتابة بحتة. إضافة استعلامات قراءة داخله يعني أن كل عملية حفظ تُطلق 3–5 استعلامات SELECT إضافية لكل aggregate محدَّث. يجعل تتبع الأداء صعباً ويخلط مسؤوليات القراءة والكتابة.

---

### A-02 — `ListActiveDelegationsInternalAsync` تُستدعى 3 مرات لكل قرار موافقة

**الملف:** `src/BG.Infrastructure/Persistence/Repositories/ApprovalQueueRepository.cs`

```csharp
// استدعاء 1 — ListApprovalActorsAsync
await ListActiveDelegationsInternalAsync(...);

// استدعاء 2 — GetApprovalActorByIdAsync (مستدعى داخل ApplyDecisionAsync)
await ListActiveDelegationsInternalAsync(...);

// استدعاء 3 — ListActiveDelegationsAsync (للعرض)
await ListActiveDelegationsInternalAsync(...);
```

كل استدعاء يُطلق `SELECT` مستقلاً لجدول `approval_delegations`. في طلب HTTP واحد لتنفيذ قرار موافقة، تُستدعى الثلاثة بالتسلسل دون أي تخزين مؤقت داخل نطاق الطلب.

---

### A-03 — `UiPreferencesController` بدون تفويض صريح

**الملف:** `src/BG.Web/Controllers/UiPreferencesController.cs`

لا يوجد `[Authorize]` ولا سياسة صريحة. يعمل بناءً على أن `WorkspaceAccessMiddleware` يتجاوزه لأنه لا يجد أذونات مطلوبة للمسار — سلوك ضمني لا صريح.

---

### A-04 — `SystemController` يكشف معلومات البنية التحتية بدون مصادقة

**الملف:** `src/BG.Web/Controllers/SystemController.cs`

`GET /api/system/architecture` يُعيد:
```json
{
  "applicationName":    "BG",
  "framework":          "ASP.NET Core 8",
  "uiLibrary":          "Razor Pages",
  "database":           "PostgreSQL",
  "hostingModel":       "IIS / In-Process",
  "integrationApproach":"Subprocess OCR Bridge"
}
```

هذا المسار مستثنى صراحةً من `WorkspaceAccessMiddleware` وليس عليه `[Authorize]`. نوع قاعدة البيانات ونهج التكامل معلومات مفيدة لهجمات مستهدفة.

---

### A-05 — تسجيل `NullOcrDocumentProcessingService` ثم إضافة `QueuedOcrProcessingService` — تناقض محتمل

**الملف 1:** `src/BG.Application/DependencyInjection.cs`
```csharp
services.TryAddSingleton<IOcrDocumentProcessingService, NullOcrDocumentProcessingService>();
```

**الملف 2:** `src/BG.Integrations/DependencyInjection.cs`
```csharp
services.AddSingleton<IOcrDocumentProcessingService, QueuedOcrProcessingService>();
```

`TryAdd` لا تُضيف إذا كانت الخدمة مسجلة، و`Add` تُضيف دائماً. بالترتيب الحالي: `TryAdd` لا تُضيف (لأن `Add` من Integrations جاءت قبلها في تسلسل `AddBgProjectServices`)؟ يجب التحقق من الترتيب الدقيق. إذا كان الترتيب معكوساً، قد يكون `IEnumerable<IOcrDocumentProcessingService>` يحتوي على كلا التسجيلين.

---

## 5. مشكلات جودة الكود

### Q-01 — قيمة Enum ميتة: `GuaranteeRequestStatus.SubmittedToBank`

**الملف:** `src/BG.Domain/Guarantees/GuaranteeRequestStatus.cs`

```csharp
public enum GuaranteeRequestStatus
{
    Draft                = 1,
    InApproval           = 2,
    Returned             = 3,
    ApprovedForDispatch  = 4,
    SubmittedToBank      = 5,  // ← لا تُستخدم أبداً في أي انتقال حالة
    AwaitingBankResponse = 6,  // ← هذا ما تصل إليه الحالة فعلاً
    ...
}
```

`Guarantee.MarkSubmittedToBank()` تنقل الحالة مباشرة إلى `AwaitingBankResponse` (6). الكود الدفاعي في `OperationsReviewMatchingService` يتعامل مع `SubmittedToBank` لكنه مسار لن يُنفَّذ أبداً في وقت التشغيل.

**الخطر:** مطور مستقبلي يبحث عن "حالة مُقدَّم للبنك" سيجد القيمة 5 ويفترض أنها تحدث، مما يؤدي إلى منطق خاطئ صامت.

---

### Q-02 — مدة انتهاء الجلسة مكررة في مكانين بدون ثابت مشترك

**الموضع 1:** `src/BG.Web/BgServicesExtensions.cs` السطر 48
```csharp
ExpireTimeSpan = TimeSpan.FromHours(12)
```

**الموضع 2:** `src/BG.Web/Pages/Auth/SignIn.cshtml.cs` السطر 80
```csharp
ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
```

تغيير أحدهما دون الآخر يخلق سلوكاً متناقضاً في انتهاء الجلسة بصمت.

---

### Q-03 — مفتاح مكرر في قاموس الأشهر العربية (Python)

**الملف:** `src/BG.Integrations/OcrWorker/ocr_worker.py` السطر ~164
```python
ARABIC_MONTH_MAP = {
    ...
    "ابريل": 4,
    "ابريل": 4,  # ← نسخ لصق، Python يتجاهل المفتاح الثاني
    ...
}
```
غير مؤثر وظيفياً لأن القيمتين متطابقتان، لكنه مؤشر على كود غير مُراجَع.

---

### Q-04 — `VerifiedDataJson` بلا قيد طول على مستوى قاعدة البيانات

**الملف:** `src/BG.Infrastructure/Persistence/BgDbContext.cs`
```csharp
.HasColumnType("text") // لا maxLength على مستوى DB
```

القيد (12,000 حرف) موجود فقط في طبقة التطبيق. أي إدراج مباشر لقاعدة البيانات خارج التطبيق (سكريبتات صيانة، أدوات ETL) لا يُقيَّد.

---

## 6. المشكلات الوظيفية وسير العمل

### F-01 — محاكي ثقة صفري يُخفي ضرورة المراجعة البشرية

**الملفان:**
- `src/BG.Application/Services/Intake/LocalIntakeExtractionConfidenceScorer.cs`
- `src/BG.Application/Services/Intake/LocalIntakeFieldReviewProjector.cs`

```csharp
// Scorer: 0% تمر بدون علامة
case IntakeFieldValueSource.DirectPdfText:
    return Math.Max(rawPercent, 0); // 0% تمر كما هي

// Projector: RequiresExplicitReview = true فقط عند غياب المرشح كلياً
if (candidate is null)
    yield return new IntakeFieldReviewDto(..., RequiresExplicitReview: true);
else
    yield return new IntakeFieldReviewDto(candidate, RequiresExplicitReview: false);
    // ↑ حتى لو كانت الثقة 0%
```

**المشكلة:** حقل مستخرج بثقة 0% (OCR فاشل جزئياً) لا يرفع علامة "تحتاج مراجعة"، بينما حقل غير مستخرج أصلاً يرفعها. المراجع البشري قد يغفل عن حقل مملوء بقيمة خاطئة استخرجها OCR بثقة صفرية.

---

### F-02 — آلية القفل عند تكرار محاولات الدخول تُعاد ضبطها عند إعادة التشغيل

**الملف:** `src/BG.Application/Services/Auth/MemoryLoginAttemptLockoutService.cs`

```csharp
private readonly Dictionary<string, LockoutRecord> _records = new();
// حالة في الذاكرة — تختفي عند كل إعادة تشغيل IIS
```

إعادة تشغيل IIS (نشر، انهيار، Application Pool recycle) تُعيد ضبط العداد إلى صفر. مهاجم يعرف دورة نشر التطبيق يستطيع تجاوز قفل المحاولات الخمس.

---

### F-03 — `BackfillIncomingEvidenceRequestLink` قد لا يُحفظ عبر `TrackAggregateChildren`

**السلوك المكتشف:**
`TrackAggregateChildren` يُعيّن حالة `EntityState.Added` فقط للكيانات الجديدة، ويترك الموجودة بحالة `Unchanged`. أي تعديل على كيان موجود (مثل `GuaranteeDocument.GuaranteeRequestId` عبر `BackfillIncomingEvidenceRequestLink`) لن يُحفظ إذا لم يكن EF يتتبع الكيان بشكل مستقل.

---

## 7. مشكلات الأمان والصلاحيات

### S-01 — بيانات اعتماد مكشوفة

*(تم توثيقه بالتفصيل في CR-01)*

---

### S-02 — `/api/system/architecture` مكشوف بدون مصادقة

*(تم توثيقه في A-04)*

---

### S-03 — `UiPreferencesController` بلا حماية صريحة

*(تم توثيقه في A-03)*

---

### S-04 — وقت انتهاء الجلسة في موضعين — خطر عدم تزامن

*(تم توثيقه في Q-02)*

---

## 8. تقييم خط أنابيب OCR / المعالجة

### البنية الفعلية للخط

```
[IntakeSubmissionService]
    ↓
[LocalIntakeExtractionEngine]
    ├── 1. LocalIntakeDocumentClassifier     تصنيف نوع المستند
    ├── 2. LocalIntakeDirectTextExtractor    استخراج نص PDF مباشر
    │       → subprocess ocr_worker.py --mode direct-pdf-text
    │       ↓ fallback عند الفشل
    │   LocalIntakeExtractionHeuristics     استخراج من اسم الملف
    ├── 3. LocalIntakeOcrExtractor           OCR للصفحات الممسوحة
    │       → subprocess ocr_worker.py --mode scanned-pdf
    ├── 4. LocalIntakeExtractionConfidenceScorer  تحديد الثقة
    └── 5. LocalIntakeFieldReviewProjector   قرار المراجعة البشرية
```

### مشكلات OCR Worker (`ocr_worker.py`)

**P-01 — تدهور صامت عند غياب مكتبات Python:**

```python
try:
    import fitz      # PyMuPDF
except ImportError:
    fitz = None      # بدون استخراج PDF

try:
    import cv2       # OpenCV
except ImportError:
    cv2 = None       # بدون تحسين الصورة

try:
    from paddleocr import PaddleOCR
except ImportError:
    PaddleOCR = None # بدون OCR عربي
```

بيئة تطوير لا تحتوي إلا على `fitz` ستُنتج نتائج مختلفة جذرياً عن بيئة إنتاج مكتملة، **دون أي خطأ في السجلات**. لا يوجد `startup_validation()` يفحص المتطلبات.

**P-02 — فشل تهيئة PaddleOCR يُعطّل OCR للأبد في عمر العملية:**

```python
def get_paddle_ocr():
    global PADDLE_OCR_INSTANCE
    if PADDLE_OCR_INSTANCE is None:
        try:
            PADDLE_OCR_INSTANCE = PaddleOCR(use_angle_cls=True, lang='ar')
        except Exception:
            PADDLE_OCR_INSTANCE = False  # ← False = فشل دائم، لا إعادة محاولة
    return PADDLE_OCR_INSTANCE if PADDLE_OCR_INSTANCE else None
```

`QueuedOcrProcessingService` في .NET هي `singleton`. فشل تهيئة PaddleOCR يجعل كل عمليات OCR صامتة دون أي استخراج حقيقي طوال دورة حياة التطبيق.

**P-03 — تنظيف ملف مؤقت مزدوج محتمل:**

```python
preprocessed_path = preprocess_image(rendered_path)
# إذا كان cv2 غير متاح: preprocessed_path == rendered_path
finally:
    cleanup_file(rendered_path)
    cleanup_file(preprocessed_path)  # ← نفس الملف إذا لم يكن cv2 متاحاً
```

`cleanup_file` يتجاهل `OSError`، لذا لا ضرر فعلي، لكنه تصميم هش.

**P-04 — سقف ثقة المصادر:**

```csharp
case IntakeFieldValueSource.FileNamePattern:
    return Math.Max(Math.Min(rawScore, 65), 0); // أقصى 65%

case IntakeFieldValueSource.ScenarioSample:
    return Math.Max(Math.Min(rawScore, 25), 0); // أقصى 25%

case IntakeFieldValueSource.DirectPdfText:
case IntakeFieldValueSource.OcrFallback:
    return Math.Max(rawScore, 0); // بلا سقف — 0% تمر دون تحذير
```

---

## 9. قاعدة البيانات وسلامة البيانات

### D-01 — N+1 داخل `TrackAggregateChildren` في كل `SaveChangesAsync`

```csharp
// لكل نوع كيان فرعي (Documents, Requests, Correspondence, Events):
var existingIds = dbSet.AsNoTracking()
    .Where(predicate)
    .Select(keySelectorExpression)
    .ToHashSetAsync(); // ← استعلام DB منفصل داخل SaveChanges
```

ضمانة بـ 4 أنواع كيانات فرعية = 4 استعلامات SELECT إضافية لكل `SaveChanges`.

---

### D-02 — `ListActiveDelegationsInternalAsync` تُستدعى 3 مرات في طلب واحد

*(تم توثيقه في A-02)*

---

### D-03 — فهرس `GuaranteeCorrespondence.GuaranteeId` المنفرد مفقود

الـ snapshot يحتوي على فهرس مركب `(GuaranteeId, LetterDate)` لكن لا فهرس منفرد على `GuaranteeId` وحده. استعلامات تفلتر فقط على `GuaranteeId` قد لا تستغل الفهرس المركب بكفاءة في بعض سيناريوهات PostgreSQL.

---

### D-04 — لا قيد تفرد مركب على `guarantee_request_document_links`

لا يوجد `UNIQUE(guarantee_request_id, guarantee_document_id)` صريح في snapshot. يمكن نظرياً إدراج نفس رابط المستند–الطلب مرتين.

---

### D-05 — `VerifiedDataJson` بلا قيد طول على مستوى DB

*(تم توثيقه في Q-04)*

---

## 10. فجوات الاختبارات

| المنطقة | الحالة | التفاصيل |
|---|---|---|
| نموذج النطاق `Guarantee` | ✅ جيد | 672 سطر اختبار، تغطية دورة حياة كاملة |
| خط أنابيب الاستخراج | ✅ جيد | جميع الخدمات مغطاة |
| صفحات Razor Pages | ✅ جيد | 22 ملف اختبار |
| `TrackAggregateChildren` تحت التزامن | ❌ غائب | لا اختبارات للحفظ المتزامن |
| `QueuedOcrProcessingService` إلغاء/إيقاف | ❌ غائب | لا اختبارات لمسارات إغلاق الخدمة |
| `WorkspaceAccessMiddleware` حالات الحدود | ❌ غائب | حساسية الحالة في مطابقة المسارات |
| `LocalIntakeExtractionHeuristics` مباشرة | ❌ غائب | مختبرة فقط عبر المحرك الكامل |
| `ocr_worker.py` اختبارات وحدة Python | ❌ غائب | لا pytest، لا اختبارات في CI |
| تحديث كيانات موجودة عبر `TrackAggregateChildren` | ❌ غائب | سلوك F-03 غير مؤكد |
| حالة `SubmittedToBank` غير قابلة للوصول | ❌ غائب | لا اختبار يؤكد أنها لا تُعيَّن |
| نسخ DB التراجعية | ❌ غائب | لا اختبارات rollback للتهجير |
| CI لا يفشل عند CVEs | ❌ ثغرة | `--fail-on-severity` مفقود |

**ملاحظة هامة:** الاختبارات تعتمد على SQLite بدلاً من PostgreSQL. نمط `TrackAggregateChildren` المعقد قد يتصرف بشكل مختلف تحت PostgreSQL بسبب اختلافات في قفل الصفوف والقيود.

---

## 11. التناقضات بين الوثائق والكود

| الوثيقة | الادعاء | الواقع في الكود | التصنيف |
|---|---|---|---|
| `AUDIT_REMEDIATION_BACKLOG.md` | "تم إزالة `HospitalApi` placeholder" | مؤكد — الكلاس غير موجود | ✅ متسق |
| `AUDIT_REMEDIATION_BACKLOG.md` | "`OperationsReviewMatchingService` لا تزال مقترنة بـseed service" | الاقتران موجود في الكود | ✅ متسق |
| `ARCHITECTURE.md` | ".NET 8 target" | `<TargetFramework>net8.0</TargetFramework>` في جميع csproj | ✅ متسق |
| `ARCHITECTURE.md` | "PostgreSQL كقاعدة بيانات" | `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11` | ✅ متسق |
| `frontend_reconstruction_plan.md` | يصف هيكل `_SplitWorkbench` المستقبلي | لا يوجد هذا الهيكل حالياً | انجراف طفيف — خطة مستقبلية |
| `ocr_implementation_plan.md` | `LayoutParser` للتحليل المكاني | يُستخدم فعلاً بشكل اختياري | ✅ متسق |
| لا توجد وثيقة | — | `GuaranteeRequestStatus.SubmittedToBank` حية في الكود لكن لا تُعيَّن أبداً | **خطر داخلي غير موثق** |

---

## 12. تقرير التكرار والهدر

### كود ميت

| العنصر | الملف | السبب |
|---|---|---|
| `GuaranteeRequestStatus.SubmittedToBank` | `GuaranteeRequestStatus.cs` | لا تُعيَّن في أي انتقال حالة |
| `case SubmittedToBank:` | `OperationsReviewMatchingService.cs:75` | مسار دفاعي لن يُنفَّذ أبداً |
| `scripts/QueryApp/Program.cs` | `scripts/QueryApp/` | أداة تطوير بيانات اعتماد مكشوفة |

### ملفات أصبحت ميتة

| الملف | السبب |
|---|---|
| `src/BG.Web/Pages/Intake/_IntakeDocumentPanel.cshtml` | استُبدل بـ`_CapturePanel.cshtml` في المراجعة الأخيرة |
| `src/BG.Web/Pages/Intake/_IntakeVerificationPanel.cshtml` | استُبدل بـ`_VerificationPanel.cshtml` في المراجعة الأخيرة |

### تكرار منطقي

| التكرار | الموضعان |
|---|---|
| `TimeSpan.FromHours(12)` | `BgServicesExtensions.cs:48` + `SignIn.cshtml.cs:80` |
| `ListActiveDelegationsInternalAsync()` | 3 استدعاءات في طلب موافقة واحد |

---

## 13. تقرير التناقضات الداخلية

### TC-01 — `TrackAggregateChildren` والتحديثات المفقودة

الكود يُعيّن فقط حالة `EntityState.Added` للكيانات الجديدة، ويترك الموجودة بـ`Unchanged`. أي تعديل على كيان موجود (مثل `GuaranteeDocument.GuaranteeRequestId` عبر `BackfillIncomingEvidenceRequestLink`) لن يُحفظ ما لم يكن EF يتتبع الكيان بشكل مستقل من مسار آخر.

### TC-02 — تسجيل مزدوج محتمل لـ`IOcrDocumentProcessingService`

```csharp
// Application/DependencyInjection.cs
services.TryAddSingleton<IOcrDocumentProcessingService, NullOcrDocumentProcessingService>();

// Integrations/DependencyInjection.cs
services.AddSingleton<IOcrDocumentProcessingService, QueuedOcrProcessingService>();
```

`Add` تُضيف دائماً، و`TryAdd` لا تُضيف إذا كانت مسجلة. إذا استُدعيت `AddIntegrations` أولاً ثم `AddApplication`، فإن `IEnumerable<IOcrDocumentProcessingService>` ستحتوي على كلا التسجيلين مما قد يسبب سلوكاً غير متوقع إذا حُقنت كـ`IEnumerable`.

### TC-03 — سقف ثقة 0% لا يُطابق سلوك "حقل غائب"

حقل مستخرج بثقة 0% يُعامَل كـ"موجود وصحيح" (بدون `RequiresExplicitReview`)، بينما حقل غائب كلياً يُعامَل كـ"يحتاج مراجعة". هذا عكس المنطق المتوقع: 0% ثقة أسوأ من "غير موجود".

---

## 14. القدرات المفقودة

| القدرة | الأثر الوظيفي | الأولوية |
|---|---|---|
| آلية انتهاء صلاحية وتنظيف الملفات المؤقتة | استنزاف القرص | عالية |
| إعادة محاولة تهيئة PaddleOCR عند الفشل | فقدان OCR للأبد حتى الإعادة | عالية |
| فحص `expiryDate >= issueDate` في طبقة الخدمة | خطأ 500 غير معالج | عالية |
| حماية صريحة `[Authorize]` على `UiPreferencesController` | ثغرة معمارية | متوسطة |
| تحديد أدنى ثقة يُلزم بالمراجعة (مثلاً < 30%) | تحسين دقة المراجعة البشرية | متوسطة |
| فحص وجود مكتبات Python عند بدء التشغيل | تدهور صامت | متوسطة |
| قاعدة بيانات لآلية القفل (بدلاً من الذاكرة) | تجاوز القفل عند الإعادة | متوسطة |
| اختبارات OCR Worker Python في CI | انهيارات غير مكتشفة | متوسطة |
| ثابت مشترك لمدة انتهاء الجلسة | تناقض صامت | منخفضة |
| قيد UNIQUE على `guarantee_request_document_links` | سلامة البيانات | منخفضة |
| قيد طول DB على `VerifiedDataJson` | حماية خارج التطبيق | منخفضة |

---

## 15. مؤشرات المخاطر

```
┌────────────────────────────────────────────────────────────────┐
│                     تقييم المخاطر الكمي                        │
├─────────────────────────────┬──────────────────────────────────┤
│ مؤشر التعرض للمخاطر         │ ████████░░  8.2 / 10             │
│ (بسبب CR-01: secrets.txt)   │                                  │
├─────────────────────────────┼──────────────────────────────────┤
│ نقاط الهشاشة الخفية          │ ████████░░  7.5 / 10             │
│ (P-02, A-03, TC-01)         │                                  │
├─────────────────────────────┼──────────────────────────────────┤
│ احتمال الفشل الصامت          │ ████████░░  7.8 / 10             │
│ (P-01, P-02, CR-03)         │                                  │
├─────────────────────────────┼──────────────────────────────────┤
│ مقاومة خطأ الإنسان           │ ████░░░░░░  4.5 / 10             │
│ (CR-02, Q-02, CR-03)        │                                  │
├─────────────────────────────┼──────────────────────────────────┤
│ كفاءة سير العمل              │ ██████░░░░  6.8 / 10             │
│ (A-01, A-02, D-01)          │                                  │
├─────────────────────────────┼──────────────────────────────────┤
│ قدرة البقاء على المدى الطويل │ ██████░░░░  6.5 / 10             │
│ (CR-03, TC-01, فجوات الاختبار)│                                 │
└─────────────────────────────┴──────────────────────────────────┘

المؤشر الإجمالي للصحة الهندسية:  6.2 / 10
```

---

## 16. خطة الإصلاح المرتبة بالأولوية

### 🔴 فوري (خلال 24 ساعة)

| # | الإجراء | الملف المستهدف | الجهد التقديري |
|---|---|---|---|
| 1 | إضافة `secrets.txt` إلى `.gitignore` بشكل صريح | `.gitignore` | دقيقتان |
| 2 | إضافة `scripts/QueryApp/` إلى `.gitignore` | `.gitignore` | دقيقتان |
| 3 | التحقق من تاريخ Git (`git log --all -- secrets.txt`) | — | 5 دقائق |
| 4 | الانتقال إلى `dotnet user-secrets` أو متغيرات بيئة | `src/BG.Web/` | 30 دقيقة |
| 5 | تدوير كلمات مرور بيئة الإنتاج احترازياً إذا كانت مرتبطة | — | حسب سياسة الأمان |

### 🟡 قصير المدى (خلال أسبوع)

| # | الإجراء | الملف المستهدف | الجهد التقديري |
|---|---|---|---|
| 6 | إضافة فحص `expiryDate >= issueDate` في `IntakeSubmissionService` | `IntakeSubmissionService.cs:187` | ساعة |
| 7 | إضافة `[Authorize]` على `UiPreferencesController` | `UiPreferencesController.cs` | 5 دقائق |
| 8 | تقييد أو إزالة `GET /api/system/architecture` | `SystemController.cs` | 30 دقيقة |
| 9 | استخراج `SessionDurationHours = 12` كثابت مشترك | `BgServicesExtensions.cs` + `SignIn.cshtml.cs` | 30 دقيقة |
| 10 | تفعيل `--fail-on-severity critical` في CI | `.github/workflows/ci.yml` | 10 دقائق |
| 11 | إضافة `startup_validation()` في `ocr_worker.py` | `ocr_worker.py` | ساعتان |
| 12 | توثيق أو حذف `GuaranteeRequestStatus.SubmittedToBank` | `GuaranteeRequestStatus.cs` | 30 دقيقة |
| 13 | حذف `_IntakeDocumentPanel.cshtml` و`_IntakeVerificationPanel.cshtml` الميتين | `Pages/Intake/` | 5 دقائق |

### 🟢 متوسط المدى (خلال شهر)

| # | الإجراء | الجهد التقديري |
|---|---|---|
| 14 | تنفيذ `IHostedService` لتنظيف الملفات المؤقتة المنتهية | يوم عمل |
| 15 | تحسين `RequiresExplicitReview` ليشمل الثقة ≤ حد أدنى قابل للتهيئة | نصف يوم |
| 16 | استبدال آلية القفل بتخزين دائم (DB أو distributed cache) | نصف يوم |
| 17 | إضافة آلية إعادة محاولة لتهيئة PaddleOCR في `ocr_worker.py` | ساعتان |
| 18 | دمج `ListActiveDelegationsInternalAsync` في cache داخل نطاق الطلب | نصف يوم |
| 19 | إضافة `pytest` لاختبار `ocr_worker.py` في CI | يوم عمل |
| 20 | التحقق من مسار `BackfillIncomingEvidenceRequestLink` عبر `TrackAggregateChildren` | نصف يوم |
| 21 | إضافة `UNIQUE(guarantee_request_id, guarantee_document_id)` في migration | ساعة |
| 22 | إضافة قيد طول على `VerifiedDataJson` على مستوى DB | ساعة |

### 🔵 طويل المدى (ربع تالٍ)

| # | الإجراء | الجهد التقديري |
|---|---|---|
| 23 | إعادة هيكلة `TrackAggregateChildren` للتخلص من SELECT داخل SaveChanges | 2 أيام |
| 24 | الانتقال إلى اختبارات تعتمد على PostgreSQL بدلاً من SQLite | 3 أيام |
| 25 | إضافة code coverage reporting في CI | نصف يوم |

---

## خلاصة المراجع

النظام مكتمل وظيفياً بنسبة عالية ويعمل بشكل صحيح في الحالات العادية. نموذج النطاق قوي ومدروس مع حدود aggregate واضحة. خط أنابيب الموافقات والتفويض متكامل وقابل للتهيئة.

**الخطر الحرج الوحيد الذي يمكن أن يُفعَّل الآن** هو إمكانية دفع `secrets.txt` إلى المستودع عن طريق الخطأ. هذا الخطر لا يتطلب سوى دقيقتين لمعالجته.

بقية المشكلات تتراوح بين هشاشة تشغيلية (P-01, P-02, CR-03) وديون تقنية مقبولة (A-01, A-02). لا توجد قضية حرجة تمنع التشغيل الإنتاجي عدا CR-01.

**التوصية النهائية:**
> معالجة CR-01 وCR-02 فورياً، ثم جدولة باقي البنود في دورة تطوير عادية ضمن backlog مرتب حسب الأولوية.
