# خطة تنفيذ منظومة استخراج هجينة (Hybrid Extraction) لنظام BG

## 1. الملخص التنفيذي
هذه الخطة تهدف إلى رفع جودة استخراج بيانات الضمانات من المستندات (PDF/Scan) من وضع تجريبي قابل للأخطاء إلى وضع تشغيلي Production-Grade.

النهج المقترح ليس OCR فقط، بل منظومة هجينة تتكون من:
- OCR + قراءة النص المباشر من PDF
- قواعد نمطية (Regex/Heuristics)
- طبقة تحقق منطقي للحقول
- درجات ثقة لكل حقل
- Human-in-the-loop للمراجعة
- دورة تحسين مستمرة مبنية على تصحيحات المستخدمين

## 2. الأهداف
### 2.1 أهداف العمل (Business)
- تقليل الإدخال اليدوي للبيانات في مسار الإدخال.
- تقليل أخطاء الاستخراج المؤثرة على القرار.
- تقليل زمن المعالجة لكل ملف.
- رفع موثوقية النظام لدى المستخدم النهائي.

### 2.2 أهداف تقنية (Technical)
- بناء خط استخراج متعدد المصادر مع ترجيح ذكي للنتائج.
- فرض تحقق منطقي قبل تعبئة أي حقل تلقائيا.
- إنشاء نظام قياس دقة على مستوى الحقل (Field-level accuracy).
- تحويل التصحيحات البشرية إلى مادة تحسين مستمر.

## 3. نطاق الحقول (MVP)
الحقول الحرجة للضمان الجديد:
- رقم الضمان
- اسم البنك
- المستفيد
- العميل/المقاول
- تصنيف الضمان
- المبلغ
- رمز العملة
- تاريخ الإصدار
- تاريخ الانتهاء

حقول إضافية (سيناريوهات لاحقة):
- تاريخ الخطاب الرسمي
- تاريخ الانتهاء الجديد
- المرجع البنكي
- بيان الحالة

## 4. معايير الجودة والقبول
### 4.1 مؤشرات الأداء (KPIs)
- دقة رقم الضمان >= 95%
- دقة المبلغ >= 92%
- دقة التاريخ (إصدار/انتهاء) >= 90%
- دقة البنك >= 97%
- دقة المستفيد/المقاول >= 90%
- زمن استخراج P95 <= 45 ثانية للملف الأحادي
- نسبة الملفات التي تحتاج مراجعة بشرية <= 35% في نهاية المرحلة الأولى

### 4.2 سياسة القبول الآلي
- الحقول عالية الحساسية (رقم الضمان، المبلغ، التواريخ):
  - لا تُقبل تلقائيا إلا إذا تجاوزت عتبة ثقة + تحقق منطقي.
- إذا فشل التحقق: الحقل يبقى فارغا مع وسم "تتطلب مراجعة".

## 5. البيانات المطلوبة (Ground Truth)
### 5.1 حجم العينة المبدئي
- 200 إلى 300 مستند كدفعة أولى.
- تمثيل كل بنك ونمط خطاب (ضمان جديد، تمديد، تخفيض، إفادة، مرفق).
- تنويع جودة المسح (واضح، متوسط، ضعيف) مع عينات حقيقية.

### 5.2 صيغة التوسيم
لكل مستند:
- file_id
- scenario_type
- bank_name
- fields:
  - guarantee_number
  - amount
  - currency
  - issue_date
  - expiry_date
  - beneficiary
  - principal
  - category
- reviewer_id
- review_timestamp

### 5.3 سياسة الجودة للوسم
- عينة تحقق مزدوج (Double annotation) بنسبة 10-15%.
- توثيق قواعد التوسيم في دليل داخلي واحد.

## 6. التصميم المستهدف للمنظومة
### 6.1 خط المعالجة
1) استقبال الملف
2) Pre-processing (تنظيف الصورة/تصحيح الميل)
3) Document classification (السيناريو والبنك/النموذج)
4) Multi-source extraction:
   - Direct PDF text
   - OCR extraction
   - Pattern extraction
5) Candidate scoring and fusion
6) Validation layer
7) Confidence gating
8) Output to UI + human review flags
9) Feedback capture
10) Continuous improvement

### 6.2 مصادر الاستخراج
- المصدر 1: النص المباشر من PDF (إن وجد)
- المصدر 2: OCR من الصور/الصفحات الممسوحة
- المصدر 3: قواعد نمطية وسياقية لكل حقل
- المصدر 4 (اختياري لاحقا): نموذج ML مخصص للحقول الصعبة

### 6.3 دمج النتائج (Fusion)
- لكل حقل تُجمع كل المرشحات (Candidates)
- كل مرشح يحصل على:
  - confidence
  - source
  - validation status
- يتم اختيار أفضل مرشح مقبول منطقيا

## 7. طبقة التحقق المنطقي
### 7.1 قواعد عامة
- تاريخ الانتهاء >= تاريخ الإصدار
- العملة ضمن قائمة مدعومة
- المبلغ رقم موجب ضمن نطاق منطقي
- رقم الضمان لا يطابق أنماط السجل التجاري/أرقام الوحدات
- التطابق مع نوع السيناريو (مثال: تمديد يحتاج NewExpiryDate)

### 7.2 مبدأ الأمان
- Better empty than wrong:
  - إذا هناك شك، لا نعبئ القيمة تلقائيا.

## 8. تجربة المستخدم (Human-in-the-loop)
- لكل حقل في الشاشة:
  - القيمة المقترحة
  - نسبة الثقة
  - مصدر القيمة
  - حالة المراجعة (مقبول/يتطلب مراجعة)
- أولوية المراجعة للحقول الحرجة.
- تسهيل التعديل اليدوي بسرعة.

## 9. القياس والمراقبة
### 9.1 لوحات المتابعة
- دقة كل حقل لكل بنك ولكل سيناريو
- زمن الاستخراج P50/P95
- نسبة timeout/failure
- نسبة الحقول منخفضة الثقة

### 9.2 تنبيهات تشغيلية
- انخفاض مفاجئ في دقة حقل حرج
- ارتفاع زمن المعالجة
- ارتفاع نسبة fallback أو القيم الفارغة

## 10. خطة التنفيذ الزمنية
## المرحلة 0: التحضير (3-5 أيام)
- توحيد تعريف الحقول الحرجة
- تحديد عتبات الثقة
- إعداد هيكل Ground Truth

## المرحلة 1: Quick Wins (أسبوع)
- تقوية validation layer
- منع التعبئة الخاطئة للحقول الحرجة
- تحسين رسائل timeout/queue
- إعداد أول Dashboard بسيط

## المرحلة 2: Hardening (أسبوعان)
- تحسين قواعد extraction الخاصة بالبنوك الأكثر تكرارا
- تحسين الدمج بين المصادر
- تحسين الأداء لتقليل زمن المعالجة

## المرحلة 3: Learning Loop (أسبوعان)
- ربط تصحيحات المستخدمين ببيانات تدريب/تحسين
- دورات تحسين أسبوعية مبنية على أخطاء حقيقية

## 11. المتطلبات التشغيلية
- موارد حسابية كافية لعمليات OCR
- ضبط Timeout واقعي لكل بيئة
- إدارة Queue واضحة
- آلية retry آمنة
- Logging تفصيلي للحقول (دون تسريب بيانات حساسة)

## 12. المخاطر وخطط التخفيف
### خطر 1: جودة مسح منخفضة
- تخفيف: Preprocessing أفضل + اشتراط حد أدنى للجودة

### خطر 2: اختلاف قوالب البنوك
- تخفيف: Rules per bank + bank detection + fallback آمن

### خطر 3: زمن استخراج مرتفع
- تخفيف: تحسين pipeline، warmup models، queue control، timeout policy

### خطر 4: ثقة زائفة بقيم خاطئة
- تخفيف: Validation + confidence gating + mandatory review للحقول الحرجة

## 13. تعريف النجاح (Definition of Done)
- نظام الاستخراج ينتج حقول موثوقة قابلة للتشغيل على عينات واقعية.
- جميع الحقول الحرجة تخضع لسياسة قبول واضحة.
- تتوفر لوحة قياس دقة وزمن تشغيل.
- توجد آلية تحسين مستمر معتمدة على التصحيحات البشرية.

## 14. خطة عمل فورية (Next Actions)
1) اعتماد هذه الخطة كمرجع رسمي.
2) بدء جمع Ground Truth (الدفعة الأولى).
3) تثبيت عتبات الثقة والقبول الآلي لكل حقل.
4) تنفيذ Quick Wins في الكود الحالي.
5) إصدار تقرير baseline قبل/بعد بالأرقام.

## 15. Execution Blueprint (الطبقة التنفيذية الإلزامية)
هذا القسم إلزامي للتنفيذ البرمجي. الهدف منه منع أي تنفيذ عشوائي عبر فرض:
- Data Contracts واضحة
- State Object موحد
- Execution Order صريح
- فصل مسؤوليات لا يسمح بالخلط بين OCR وValidation وUI

### 15.1 Data Contracts (إجباري)
```csharp
public enum CandidateSource
{
    DirectPdfText,
    Ocr,
    Regex,
    Heuristic,
    Model
}

public sealed class FieldCandidate
{
    public required string FieldName { get; init; }
    public required string Value { get; init; }
    public required double Confidence { get; init; } // 0..100
    public required CandidateSource Source { get; init; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public string? Evidence { get; init; } // matched text or rule name
}

public sealed class FinalFieldResult
{
    public required string FieldName { get; init; }
    public string? Value { get; init; }
    public required double Confidence { get; init; }
    public required CandidateSource? Source { get; init; }
    public required bool RequiresReview { get; init; }
    public string? Reason { get; init; }
}
```

### 15.2 Document Processing State (قلب النظام)
```csharp
public sealed class DocumentProcessingContext
{
    public required string FileId { get; init; }
    public required string ScenarioKey { get; init; }
    public string? DocumentFormKey { get; set; }
    public bool IsScanned { get; set; }

    public string RawDirectText { get; set; } = string.Empty;
    public string RawOcrText { get; set; } = string.Empty;

    public List<FieldCandidate> Candidates { get; } = new();
    public Dictionary<string, FinalFieldResult> FinalFields { get; } = new(StringComparer.Ordinal);

    public List<string> Logs { get; } = new();
    public List<string> Warnings { get; } = new();
}
```

### 15.3 Execution Order (لا يُسمح بتجاوزه)
```csharp
public async Task<DocumentProcessingContext> ProcessAsync(DocumentInput input, CancellationToken ct)
{
    var ctx = _contextFactory.Create(input);

    await _documentClassifier.ClassifyAsync(ctx, ct);            // Step 1
    await _textExtractor.ExtractDirectTextAsync(ctx, ct);        // Step 2
    await _ocrExtractor.ExtractOcrTextAsync(ctx, ct);            // Step 3 (if needed)

    await _candidateExtractor.ExtractCandidatesAsync(ctx, ct);   // Step 4
    await _candidateValidator.ValidateAsync(ctx, ct);            // Step 5
    await _candidateFusion.SelectBestAsync(ctx, ct);             // Step 6
    await _confidenceGate.ApplyAsync(ctx, ct);                   // Step 7

    return ctx;                                                  // Step 8
}
```

### 15.4 Required Interfaces
```csharp
public interface IDocumentClassifier
{
    Task ClassifyAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface ITextExtractor
{
    Task ExtractDirectTextAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface IOcrExtractor
{
    Task ExtractOcrTextAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface ICandidateExtractor
{
    Task ExtractCandidatesAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface ICandidateValidator
{
    Task ValidateAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface ICandidateFusion
{
    Task SelectBestAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface IConfidenceGate
{
    Task ApplyAsync(DocumentProcessingContext ctx, CancellationToken ct);
}
```

### 15.5 Responsibility Boundaries (Constraint Architecture)
- OCR Layer:
  - تنتج نص خام + شواهد فقط.
  - ممنوع اختيار الحقل النهائي داخلها.
- Candidate Extraction Layer:
  - تنتج Candidates فقط.
  - ممنوع كتابة FinalFields.
- Validation Layer:
  - تتحقق من Candidates وتحدد IsValid/Message.
  - ممنوع قراءة/تعديل UI.
- Fusion Layer:
  - تختار أفضل Candidate صالح لكل حقل.
  - لا تنفذ Regex/OCR.
- Confidence Gate:
  - تطبق سياسات القبول الآلي أو RequiresReview.
  - هي الجهة الوحيدة المسموح لها إسقاط القيم منخفضة الثقة.
- UI Layer:
  - تعرض FinalFields فقط.
  - ممنوع تطبيق قواعد استخراج داخل الواجهة.

### 15.6 Mandatory Acceptance Rules (Field Level)
- GuaranteeNumber:
  - Accept if Confidence >= 95 and passes pattern + anti-CR checks.
  - Else: null + RequiresReview=true.
- Amount:
  - Accept if Confidence >= 92 and parsed decimal > 0 and range valid.
  - Else: null + RequiresReview=true.
- IssueDate/ExpiryDate:
  - Accept if date format valid and ExpiryDate >= IssueDate.
  - Else: null + RequiresReview=true.

### 15.7 Logging Contract
لكل مرحلة يجب تسجيل:
- StageStart/StageEnd
- DurationMs
- CandidateCountAdded
- ValidationRejectedCount
- FinalAcceptedCount

ملاحظة: لا يسمح بتسجيل بيانات حساسة خام في logs الإنتاجية.

### 15.8 Folder Structure (Suggested)
```text
src/BG.Application/Extraction/
  Contracts/
    DocumentProcessingContext.cs
    FieldCandidate.cs
    FinalFieldResult.cs
    Interfaces/*.cs
  Pipeline/
    DocumentProcessingPipeline.cs
  Stages/
    DocumentClassifier.cs
    DirectTextExtractor.cs
    OcrExtractor.cs
    CandidateExtractor.cs
    CandidateValidator.cs
    CandidateFusion.cs
    ConfidenceGate.cs
  Rules/
    GuaranteeNumberRules.cs
    AmountRules.cs
    DateRules.cs
```

### 15.9 Agent Execution Mode (AI-safe)
عند تكليف AI Agent، يمنع إعطاؤه الخطة العامة فقط. يجب أن يكون التكليف كالتالي:
1) Implement contracts only.
2) Implement pipeline skeleton only.
3) Implement one stage only (example: CandidateExtractor.Regex).
4) Add tests for that stage.
5) Stop.

هذا يمنع الدمج العشوائي ويضمن Buildable increments.

## 16. Backlog قابل للتنفيذ (Implementation Backlog)
### Epic 1: Contracts and Pipeline Skeleton
- Task 1.1: Add FieldCandidate, FinalFieldResult, DocumentProcessingContext
- Task 1.2: Add stage interfaces
- Task 1.3: Add DocumentProcessingPipeline with strict order
- Task 1.4: Unit tests for pipeline order

### Epic 2: Candidate Extraction and Validation
- Task 2.1: Implement direct text candidate extractor
- Task 2.2: Implement OCR candidate extractor
- Task 2.3: Implement validation rules (guarantee/amount/date)
- Task 2.4: Unit tests per rule

### Epic 3: Fusion and Confidence Gate
- Task 3.1: Implement candidate ranking
- Task 3.2: Implement field-level acceptance thresholds
- Task 3.3: Implement RequiresReview outputs
- Task 3.4: Integration tests end-to-end

### Epic 4: UI + Observability
- Task 4.1: Bind UI to FinalFields only
- Task 4.2: Display source/confidence/review-state
- Task 4.3: Add stage telemetry and dashboard metrics

## 17. Definition of Ready for Any Coding Task
أي مهمة لا تبدأ قبل توفر هذه العناصر:
- Input contract
- Output contract
- Stage owner
- Unit test criteria
- Acceptance threshold

إذا عنصر واحد مفقود، تعتبر المهمة غير جاهزة (Not Ready).
