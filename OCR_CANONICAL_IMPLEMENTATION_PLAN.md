# الخطة الرسمية الموحدة لتنفيذ OCR في BG (Canonical Implementation Plan)

**الإصدار:** 1.0
**التاريخ:** 2026-04-06
**الحالة:** مرجع التنفيذ الرسمي (Single Source of Truth)

## 1) الهدف من هذا المستند
هذا المستند هو المرجع الوحيد للتنفيذ. تم إنشاؤه لدمج:
- الرؤية المعمارية (Hybrid)
- العمق التقني (Ensemble)
- صرامة التنفيذ المرحلي (Master)

ويعالج ملاحظات المراجعة الخارجية بشكل صريح:
- إزالة التناقض بين المستندات
- ضبط الأداء والموارد
- تحديد عقد البيانات بين C# وPython
- تعريف خطط الفشل والاستعادة
- تعريف خطة واقعية للـ Ground Truth والقياس

---

## 2) قرارات ملزمة (Non-Negotiable Decisions)
أي تنفيذ يخالف هذه القرارات يعتبر خارج الخطة.

1. **مالك القرار النهائي = C# (BG.Application) فقط**
- Python worker مسؤول عن الاستخراج وإرجاع المرشحات فقط.
- لا يسمح لـ Python باعتماد القيمة النهائية أو قرار الحفظ الآلي.

2. **Python مسؤول عن Extraction فقط**
- يسمح: OCR, PDF text parsing, regex candidates, evidence.
- لا يسمح: Fusion النهائي، Confidence Gating النهائي، قرارات workflow.

3. **واجهة المستخدم تقرأ FinalFields فقط**
- ممنوع تطبيق قواعد استخراج داخل UI.

4. **مرجع واحد فقط للخطة**
- هذا الملف يحل محل أي تعارض بين الملفات السابقة.

5. **السلامة قبل الاكتمال**
- Better empty than wrong.
- الحقول الحرجة لا تُحفظ تلقائياً بدون تحقق كافٍ.

---

## 3) النطاق المرحلي (Scope by Phase)

### Phase 1 (MVP خلال 3-4 أسابيع)
- Direct PDF text + OCR fallback ذكي
- Candidate extraction
- Validation أساسي
- Human review إلزامي للحقول الحرجة
- Logging + baseline metrics

### Phase 2 (Hardening)
- Candidate fusion متحكم (داخل C#)
- Bank profiles لثلاثة بنوك الأكثر تكراراً
- تحسين quality gates

### Phase 3 (Optimization)
- توسيع bank profiles لباقي البنوك
- تحسينات الأداء والقياس
- LLM source اختياري فقط للحالات المتعارضة (غير مفعل افتراضياً)

---

## 4) الهيكل المعماري المعتمد

## 4.1 طبقتان واضحتان
1) **Orchestration Layer (C#)**
- Pipeline orchestration
- Candidate validation
- Fusion selection
- Confidence gating
- Output to UI/workflow

2) **Extraction Worker (Python)**
- Direct text extraction
- OCR extraction
- Regex/heuristic candidates
- Return structured candidates + evidence

## 4.2 تدفق التنفيذ
1. استلام الملف
2. تصنيف أولي للمستند/الصفحات
3. استخراج مباشر من PDF
4. OCR عند الحاجة (وفق سياسة الأداء)
5. بناء Candidates
6. Validation في C#
7. Fusion/Selection في C#
8. Confidence gate في C#
9. Output + review flags
10. حفظ telemetry + feedback metadata

---

## 5) عقود البيانات (Data Contracts)

## 5.1 C# Domain Contracts
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
    public string? Evidence { get; init; }
    public int? PageNumber { get; init; }
    public string? BoundingBox { get; init; }
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

## 5.2 JSON Contract v1 بين C# وPython
```json
{
  "contractVersion": "v1",
  "fileId": "string",
  "processingMeta": {
    "workerVersion": "string",
    "elapsedMs": 0,
    "warnings": ["string"]
  },
  "candidates": [
    {
      "fieldKey": "guaranteeNumber",
      "value": "23OGTE48803516",
      "confidence": 84,
      "source": "ocr",
      "evidence": "Guarantee No: 23OGTE48803516",
      "pageNumber": 1,
      "boundingBox": "x1,y1,x2,y2"
    }
  ]
}
```

## 5.3 قواعد العقد
- `contractVersion` إلزامي.
- Python يعيد `candidates` فقط.
- أي حقول نهائية (`finalFields`) أو قرار حفظ آلي في response = مرفوض.

---

## 6) Pipeline التنفيذي الإجباري (C#)
```csharp
public async Task<DocumentProcessingContext> ProcessAsync(DocumentInput input, CancellationToken ct)
{
    var ctx = _contextFactory.Create(input);

    await _documentClassifier.ClassifyAsync(ctx, ct);            // Step 1
    await _textExtractor.ExtractDirectTextAsync(ctx, ct);        // Step 2
    await _ocrExtractor.ExtractOcrTextAsync(ctx, ct);            // Step 3 (policy-based)

    await _candidateExtractor.ExtractCandidatesAsync(ctx, ct);   // Step 4
    await _candidateValidator.ValidateAsync(ctx, ct);            // Step 5
    await _candidateFusion.SelectBestAsync(ctx, ct);             // Step 6
    await _confidenceGate.ApplyAsync(ctx, ct);                   // Step 7

    return ctx;                                                  // Step 8
}
```

## 6.1 فصل المسؤوليات
- OCR stage: produce raw text and candidate evidence.
- Candidate extractor: populate candidates only.
- Validator: set validity and rejection reasons.
- Fusion: choose best valid candidate per field.
- Gate: enforce auto/review policy.
- UI: render final results and review actions.

---

## 7) سياسة الأداء والموارد (Realistic Performance Policy)

## 7.1 سياسة تشغيل OCR
في Phase 1:
- لا يُشغّل OCR على كل صفحة نصية بشكل افتراضي.
- OCR يُشغّل عندما:
  - direct text ضعيف
  - أو الحقول الحرجة مفقودة
  - أو الصفحة mixed/image-only

## 7.2 أهداف SLO واقعية
Phase 1:
- P95 <= 90 ثانية (ملف أحادي متوسط)
- Timeout rate < 8%

Phase 2:
- P95 <= 60 ثانية
- Timeout rate < 5%

Phase 3:
- P95 <= 45 ثانية
- Timeout rate < 3%

## 7.3 إدارة الذاكرة
- worker process reuse قدر الإمكان
- limit concurrent OCR workers
- عند ضغط الذاكرة: degrade mode (Direct text only + mandatory review)

---

## 8) Validation & Gating Policy

## 8.1 حقول حرجة
- `guaranteeNumber`
- `amount`
- `expiryDate`

## 8.2 قواعد الحد الأدنى (Phase 1)
1. لا قبول تلقائي إذا فشل تحقق منطقي أساسي.
2. لا قبول تلقائي إذا قيمة الحقل الحرجة من مصدر واحد منخفض الثقة.
3. إذا تعارضت المصادر على حقل حرج -> RequiresReview=true.

## 8.3 قواعد تحقق أساسية
- `expiryDate >= issueDate`
- `amount > 0` وبصيغة رقمية صالحة
- `guaranteeNumber` لا يطابق نمط السجل التجاري السعودي

## 8.4 قرار الحفظ
- Phase 1: **review-required افتراضياً للحقول الحرجة**.
- يمكن التحول التدريجي للحفظ الآلي في Phase 2 بعد baseline موثوق.

---

## 9) التطبيع والمقارنة (Normalization Strategy)

## 9.1 مبدأ أساسي
لا نستخدم قيمة التطبيع للعرض النهائي. نحتفظ بقيمتين:
- `RawValue` للعرض والتدقيق
- `NormalizedValue` للمقارنة والـ fusion فقط

## 9.2 قواعد الأمان
- تطبيع التاريخ يعتمد على سياق البنك/اللغة إن توفر.
- أي ambiguity في التاريخ => RequiresReview.
- التطبيع لا يجب أن يمحو إشارات مهمة يحتاجها المراجع.

---

## 10) Failure Modes & Recovery (إلزامي)

1. **Python worker crash**
- إعادة محاولة واحدة
- إن فشل: fallback إلى direct text + review-required
- log code: `worker.crash`

2. **PDF parse failure**
- fallback إلى OCR page rendering
- log code: `pdf.parse.failed`

3. **OCR timeout**
- إيقاف مرحلة OCR بعد المهلة
- لا توقف الطلب كاملًا
- mark critical fields as review-required
- log code: `ocr.timeout`

4. **OOM / resource pressure**
- degrade mode
- queue throttling
- log code: `worker.oom`

5. **Invalid contract response**
- reject response
- safe fallback
- log code: `contract.invalid`

---

## 11) Ground Truth Operating Model

## 11.1 البداية الواقعية
- دفعة أولى: 120 مستند (ليس 300 من اليوم الأول)
- 3 بنوك الأكثر تكراراً
- توازن بين السيناريوهات (new/extension/reduction)

## 11.2 ملكية البيانات
- Data Owner: فريق الأعمال (تحديد القيم الصحيحة)
- QA Owner: فريق الجودة (double annotation 10%)
- Tech Owner: الفريق التقني (evaluation tooling)

## 11.3 صيغة التوسيم (لكل مستند)

- `file_id` — معرف فريد
- `bank_name` — اسم البنك
- `scenario_type` — new-guarantee / extension / reduction / ...
- `guarantee_number` — رقم الضمان الصحيح
- `amount` — المبلغ كرقم نظيف
- `currency` — SAR / USD / ...
- `issue_date` — بصيغة yyyy-MM-dd
- `expiry_date` — بصيغة yyyy-MM-dd
- `beneficiary` — اسم المستفيد
- `principal` — اسم العميل/المقاول
- `reviewer_id` — من أجرى التوسيم
- `review_timestamp` — وقت التوسيم

## 11.4 جودة التوسيم

- **Double annotation 10-15%:** مستند من كل عشرة يُوسَّم من شخصين مستقلين
- عند الاختلاف: مشرف القسم يفضّ النزاع ويوثّق السبب
- دليل توسيم موحد يُكتب قبل بدء الجمع

---

## 12) Feedback Loop قابل للتشغيل

التصحيحات لا تُطبق تلقائياً على القواعد.

دورة كل أسبوعين:
1. استخراج أعلى الحقول تصحيحاً
2. تصنيف الأخطاء (regex gap / OCR noise / bank-template mismatch)
3. إنشاء backlog قواعد محدد
4. تطبيق محدود + قياس قبل/بعد
5. اعتماد فقط إذا التحسن مثبت

---

## 13) Observability & Metrics

## 13.0 أهداف الدقة المستهدفة (KPIs)

هذه الأرقام هي الهدف النهائي — تُقاس بعد جمع Ground Truth حقيقي:

| الحقل | دقة مستهدفة | الأولوية |
| ----- | ----------- | -------- |
| رقم الضمان | >= 95% | حرج |
| المبلغ | >= 92% | حرج |
| تاريخ الإصدار / الانتهاء | >= 90% | حرج |
| اسم البنك | >= 97% | عالية |
| المستفيد / المقاول | >= 90% | عالية |
| زمن الاستخراج P95 | <= 45 ثانية | تشغيلي |
| نسبة المراجعة البشرية | <= 35% | تشغيلي |

**ملاحظة:** في Phase 1 لا تُستخدم هذه الأرقام كشرط نشر — تُستخدم لقياس الفجوة وتوجيه التحسين.

## 13.1 Metrics إلزامية
- extraction latency (P50/P95)
- timeout rate
- worker failure rate
- field accuracy by bank
- review-required ratio
- correction rate by field

## 13.2 Logging Contract
لكل Stage:
- StageStart / StageEnd
- DurationMs
- CandidatesAdded
- RejectedCount
- AcceptedCount
- ErrorCode (if any)

---

## 14) الاختبارات (Test Strategy)

## 14.1 Unit Tests
- validation rules
- fusion selection logic
- gate decisions

## 14.2 Contract Tests
- C# parser against JSON contract v1
- invalid response rejection

## 14.3 Integration Tests
- end-to-end on curated document set

## 14.4 Accuracy Evaluation
- baseline قبل أي تغيير
- مقارنة بعد كل إصدار
- لا نشر للإنتاج بدون تقرير دقة حقيقي

---

## 15) Backlog التنفيذ التفصيلي

### Epic A: Foundation (Week 1)
- Add canonical contracts in C#
- Add JSON contract v1 parser/validator
- Add pipeline skeleton + stage interfaces
- Add unit tests for pipeline order

### Epic B: Safe Extraction (Week 2)
- Integrate Python response as candidates only
- Implement core validation rules
- Implement conservative confidence gate
- Add failure-recovery handlers

### Epic C: Review & Metrics (Week 3)
- UI: show value/source/confidence/review state
- Persist review corrections metadata
- Stage metrics + dashboards

### Epic D: Controlled Hardening (Week 4+)
- Bank profile for top 3 banks
- Normalization improvements with safety checks
- Fusion tuning based on ground truth

---

## 16) سياسة تكليف AI Agent
عند التطوير بواسطة AI Agent:
1. Task واحد صغير فقط لكل دورة
2. Input/Output contract إلزامي قبل الكود
3. Tests إلزامية قبل الإغلاق
4. ممنوع التعديل خارج نطاق المرحلة الحالية
5. Stop after completion and report

---

## 17) Definition of Ready (قبل بدء أي مهمة)

أي مهمة لا تبدأ قبل توفر هذه العناصر:

1. **Input contract** — ما الذي تستقبله هذه المرحلة؟
2. **Output contract** — ما الذي تُخرجه؟
3. **Stage owner** — من المسؤول؟
4. **Unit test criteria** — كيف نتحقق من الصحة؟
5. **Acceptance threshold** — ما معيار الاكتمال؟

إذا عنصر واحد مفقود → المهمة غير جاهزة للتنفيذ.

---

## 18) Definition of Done (بعد إنهاء أي مهمة)

لا تعتبر المرحلة منتهية إلا إذا تحقق:

1. Build ناجح بدون أخطاء جديدة
2. Contract tests ناجحة
3. Integration smoke tests ناجحة
4. Metrics مرئية
5. تقرير دقة baseline/after متوفر

---

## 19) ماذا تم إيقافه عمداً في النسخة الحالية
- تشغيل OCR الإجباري على جميع الصفحات النصية
- LLM vision source في المرحلة الأولى
- Auto-learning rules مباشرة من تصحيحات المستخدمين

السبب: تقليل المخاطر وتسريع الوصول لنظام مستقر قابل للقياس.

---

## 20) Appendix: Decision Matrix (سريع)
- Final decision ownership: **C#**
- Extraction ownership: **Python**
- Contract authority: **JSON v1 in this document**
- MVP safety posture: **Review-first for critical fields**
- Performance posture: **Policy-driven OCR, not always-on**

هذا المستند هو مرجع التنفيذ الوحيد المعتمد حتى إصدار 1.1.

---

## 22) Integration Guide — دليل التكامل مع الكود الحالي

> **تحذير مهم:** الخطط الثلاث السابقة صمّمت بنية كاملة من الصفر.
> الكود الفعلي يحتوي بالفعل على معظمها. هذا القسم يصحح الصورة ويحدد بدقة ما يُضاف وما يُعدَّل.

---

### 22.1 البنية الفعلية الموجودة الآن

```text
Workspace.cshtml.cs → OnPostExtractAsync()
  └─ IIntakeSubmissionService.BeginExtractionAsync()
       └─ IIntakeExtractionEngine.ExtractAsync()       ← LocalIntakeExtractionEngine
            ├─ IIntakeDocumentClassifier.Classify()    ← LocalIntakeDocumentClassifier
            ├─ ExtractCandidatesAsync()
            │    ├─ IIntakeDirectTextExtractor          ← LocalIntakeDirectTextExtractor
            │    │    └─ يستدعي IOcrDocumentProcessingService (Python)
            │    │         ويُصفّي النتائج بـ SourceLabel == "direct-pdf-text"
            │    └─ IIntakeOcrExtractor                 ← LocalIntakeOcrExtractor
            │         └─ يستدعي IOcrDocumentProcessingService (Python)
            │              ويأخذ جميع النتائج
            └─ IIntakeFieldReviewProjector.Project()   ← LocalIntakeFieldReviewProjector
                 └─ يختار أفضل candidate لكل حقل
                      بحسب الأولوية ثم الـ ConfidencePercent
                      ويضع RequiresExplicitReview = confidence < 40
```

**الـ Models الموجودة:**
```csharp
// المرشح الموجود (داخلي)
internal sealed record IntakeExtractionFieldCandidate(
    string FieldKey,
    string Value,
    IntakeFieldValueSource Source,   // DirectPdfText=400, OcrFallback=300, ...
    int ConfidencePercent);

// النتيجة النهائية الموجودة (عامة)
public sealed record IntakeFieldReviewDto(
    string FieldKey,
    string LabelResourceKey,
    string Value,
    int ConfidencePercent,
    bool RequiresExplicitReview,
    string? ProvenanceResourceKey = null,
    bool IsExpectedByDocumentForm = false);
```

---

### 22.2 الخريطة الكاملة: ما تقوله الخطة → ما يقابله في الكود

| ما تقوله الخطة | ما يقابله فعلياً | الحالة |
| -------------- | ---------------- | ------ |
| `DocumentProcessingPipeline` | `LocalIntakeExtractionEngine` | موجود ✅ |
| `IDocumentClassifier` | `IIntakeDocumentClassifier` | موجود ✅ |
| `ICandidateExtractor` | `IIntakeDirectTextExtractor` + `IIntakeOcrExtractor` | موجود ✅ |
| `ICandidateFusion` | `LocalIntakeFieldReviewProjector` | موجود — يحتاج تعزيز 🔧 |
| `IConfidenceGate` | `LocalIntakeExtractionConfidenceScorer` | موجود جزئياً — يحتاج توسيع 🔧 |
| `FieldCandidate` | `IntakeExtractionFieldCandidate` | موجود — يحتاج `RawValue` + `IsValid` 🔧 |
| `FinalFieldResult` | `IntakeFieldReviewDto` | موجود — يحتاج `SourcesAgreed` + `ReviewDecision` 🔧 |
| `ICandidateValidator` | غير موجود | **يُنشأ جديداً** ❌ |
| `OcrFeedbackRecord` | غير موجود | **يُنشأ جديداً** ❌ |
| Cross-field validation | غير موجود | **يُضاف** ❌ |

---

### 22.3 ماذا يتغير بالضبط (لا ماذا يُنشأ من صفر)

**أولاً — IntakeExtractionFieldCandidate (إضافة حقلين):**
```csharp
// قبل:
internal sealed record IntakeExtractionFieldCandidate(
    string FieldKey, string Value,
    IntakeFieldValueSource Source, int ConfidencePercent);

// بعد:
internal sealed record IntakeExtractionFieldCandidate(
    string FieldKey,
    string Value,
    string RawValue,                  // ← جديد: القيمة قبل التطبيع للعرض
    IntakeFieldValueSource Source,
    int ConfidencePercent,
    bool IsValid = true,              // ← جديد: يُعبأ من Validation layer
    string? ValidationMessage = null); // ← جديد
```

**ثانياً — IntakeFieldReviewDto (إضافة ثلاثة حقول):**
```csharp
// بعد:
public sealed record IntakeFieldReviewDto(
    string FieldKey,
    string LabelResourceKey,
    string Value,
    string? RawValue,                        // ← جديد
    int ConfidencePercent,
    bool RequiresExplicitReview,
    string? ProvenanceResourceKey = null,
    bool IsExpectedByDocumentForm = false,
    bool SourcesAgreed = false,              // ← جديد
    string? SourcesConflictDetail = null,    // ← جديد: "DirectPdf: X | OCR: Y"
    string? ReviewReason = null);            // ← جديد: سبب طلب المراجعة
```

**ثالثاً — LocalIntakeFieldReviewProjector (تعزيز منطق الـ Fusion):**

الحالي: يختار الأعلى أولوية/confidence بدون مقارنة.
المطلوب: يُبقي الاختيار كما هو + يضيف منطق `SourcesAgreed`:

```csharp
// في Project() بعد اختيار الـ winner:
var allCandidatesForField = candidatesByKey[fieldKey]; // جميع المرشحين لهذا الحقل
var sourcesAgreed = allCandidatesForField.Count > 1
    && allCandidatesForField
        .Select(c => NormalizeForComparison(fieldKey, c.Value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == 1;

var conflictDetail = !sourcesAgreed && allCandidatesForField.Count > 1
    ? string.Join(" | ", allCandidatesForField.Select(c => $"{c.Source}: {c.RawValue}"))
    : null;
```

**رابعاً — ICandidateValidator (جديد كلياً — يُضاف قبل Projector):**
```csharp
// BG.Application/Intake/IIntakeCandidateValidator.cs
internal interface IIntakeCandidateValidator
{
    void Validate(IList<IntakeExtractionFieldCandidate> candidates);
}

// يُحقن في LocalIntakeExtractionEngine ويُستدعى بين ExtractCandidatesAsync و Project()
var candidates = await ExtractCandidatesAsync(...);
_candidateValidator.Validate(candidates);  // ← جديد
var fields = _fieldReviewProjector.Project(scenario, detectedForm, candidates);
```

**خامساً — Cross-field Validation (في LocalIntakeCandidateValidator):**
```csharp
// بعد Project() في LocalIntakeExtractionEngine:
ValidateCrossFields(fields);

private static void ValidateCrossFields(IReadOnlyList<IntakeFieldReviewDto> fields)
{
    var issueDate = GetDate(fields, IntakeFieldKeys.IssueDate);
    var expiryDate = GetDate(fields, IntakeFieldKeys.ExpiryDate);

    if (issueDate.HasValue && expiryDate.HasValue && expiryDate <= issueDate)
    {
        // وضع علامة RequiresExplicitReview = true على ExpiryDate
        // مع ReviewReason = "تاريخ الانتهاء قبل تاريخ الإصدار"
    }
}
```

---

### 22.4 نقطة تسجيل الـ Feedback

**الملف:** `Workspace.cshtml.cs`
**الـ Method:** `OnPostSaveAsync()` — بعد `_intakeSubmissionService.FinalizeAsync()` النجاح

```csharp
// في OnPostSaveAsync — بعد التأكد من النجاح:
foreach (var reviewField in ReviewFields)
{
    var submittedValue = ResolveCurrentValue(reviewField.FieldKey);
    if (submittedValue != reviewField.Value && !string.IsNullOrEmpty(reviewField.Value))
    {
        await _ocrFeedbackService.RecordAsync(new OcrFeedbackRecord
        {
            DocumentId = result.Value!.DocumentId,
            FieldKey = reviewField.FieldKey,
            ExtractedValue = reviewField.Value,
            CorrectedValue = submittedValue ?? string.Empty,
            ConfidencePercent = reviewField.ConfidencePercent,
            SourcesAgreed = reviewField.SourcesAgreed,
            RecordedAt = DateTimeOffset.UtcNow,
            RecordedByUserId = User.GetUserId()
        });
    }
}
```

---

### 22.5 Migration لـ OcrFeedbackRecords

**الاسم:** `AddOcrFeedbackRecords`
**يأتي بعد:** `20260311144129_AddIntakeDocumentMetadata`
**الجدول:** `ocr_feedback_records`

```csharp
migrationBuilder.CreateTable(
    name: "ocr_feedback_records",
    columns: table => new
    {
        id = table.Column<Guid>(nullable: false),
        document_id = table.Column<Guid>(nullable: false),
        field_key = table.Column<string>(maxLength: 64, nullable: false),
        extracted_value = table.Column<string>(maxLength: 512, nullable: false),
        corrected_value = table.Column<string>(maxLength: 512, nullable: false),
        detected_bank_name = table.Column<string>(maxLength: 128, nullable: true),
        scenario_key = table.Column<string>(maxLength: 64, nullable: true),
        confidence_percent = table.Column<int>(nullable: false),
        sources_agreed = table.Column<bool>(nullable: false),
        winning_source = table.Column<string>(maxLength: 64, nullable: true),
        recorded_at = table.Column<DateTimeOffset>(nullable: false),
        recorded_by_user_id = table.Column<string>(maxLength: 128, nullable: false)
    },
    constraints: table => table.PrimaryKey("pk_ocr_feedback_records", x => x.id));

migrationBuilder.CreateIndex(
    name: "ix_ocr_feedback_records_field_key_detected_bank",
    table: "ocr_feedback_records",
    columns: new[] { "field_key", "detected_bank_name" });
```

---

### 22.6 سلوك Phase 1 بالضبط

> **تناقض موجود بين الملفين** — هذا القسم يحسمه نهائياً:

```text
Phase 1:
  RequiresExplicitReview = true  إذا:
    - confidence < 40  (الحد الحالي — لا يتغير في Phase 1)
    - أو فشل cross-field validation
    - أو SourcesAgreed == false على حقل حرج

  AutoAccepted لا يُغلق — يُحسَب من:
    confidence >= 40 AND SourcesAgreed == true AND validation passed

Phase 2:
  ترفع عتبة RequiresExplicitReview تدريجياً
  بعد قياس baseline حقيقي من OcrFeedbackRecords
```

**لا تغيير على `ConfidenceThreshold = 40` في Phase 1** — هذا الرقم يُعاد النظر فيه بعد جمع بيانات حقيقية.

---

### 22.7 ترتيب التنفيذ المقترح (يبني على الموجود)

```text
✅ الخطوة 1: أضف RawValue + IsValid + ValidationMessage إلى IntakeExtractionFieldCandidate
✅ الخطوة 2: أضف SourcesAgreed + SourcesConflictDetail + ReviewReason إلى IntakeFieldReviewDto
✅ الخطوة 3: أنشئ IIntakeCandidateValidator + LocalIntakeCandidateValidator (قواعد فردية)
✅ الخطوة 4: عدّل LocalIntakeExtractionEngine ليستدعي Validator قبل Projector
✅ الخطوة 5: عدّل LocalIntakeFieldReviewProjector ليحسب SourcesAgreed
✅ الخطوة 6: أضف cross-field validation بعد Project()
✅ الخطوة 7: أنشئ OcrFeedbackRecord + Migration + IOcrFeedbackService
✅ الخطوة 8: أضف استدعاء RecordAsync في OnPostSaveAsync
✅ الخطوة 9: عدّل ocr_worker.py — إزالة deduplicate_fields، إضافة rawValue في make_field
✅ الخطوة 10: عدّل OcrDocumentFieldCandidateDto لاستقبال RawValue من Python
✅ الخطوة 11: عدّل LocalIntakeOcrExtractor — إزالة GroupBy الداخلي، استخدام RawValue
```

**لا تنشئ:** `DocumentProcessingPipeline` / `DocumentProcessingContext` / `ICandidateFusion` / `IConfidenceGate`
هذه مفاهيم موجودة بأسماء مختلفة في الكود.

---

## 21) المرجع التقني التفصيلي

هذا الملف يحدد **ماذا** و**لماذا**.
للتفاصيل التقنية الكاملة (**كيف**) انظر: **[OCR_UNIFIED_PLAN.md](OCR_UNIFIED_PLAN.md)**

| الموضوع | المرجع |
| ------- | ------ |
| كود Consensus Engine كامل (C#) | OCR_UNIFIED_PLAN — القسم السادس |
| كود Validation Graph كامل (C#) | OCR_UNIFIED_PLAN — القسم السابع |
| كود Confidence Gate كامل (C#) | OCR_UNIFIED_PLAN — القسم الثامن |
| تغييرات Python لـ multi-candidate output | OCR_UNIFIED_PLAN — القسم الثالث |
| عقد JSON كامل مع أمثلة | OCR_UNIFIED_PLAN — القسم الثالث |
| DocumentProcessingContext كامل | OCR_UNIFIED_PLAN — القسم الرابع |
| FinalFieldResult كامل (SourcesAgreed, Conflict) | OCR_UNIFIED_PLAN — القسم الرابع |
| جدول Failure Modes التفصيلي | OCR_UNIFIED_PLAN — القسم التاسع |
| Ground Truth CSV + سكريبت القياس | OCR_UNIFIED_PLAN — القسم العاشر |
| Feedback Loop بكود C# | OCR_UNIFIED_PLAN — القسم الحادي عشر |
| Human Review UI بالتفصيل | OCR_UNIFIED_PLAN — القسم الثاني عشر |
| Folder Structure كامل | OCR_UNIFIED_PLAN — القسم الرابع عشر |

**قاعدة:** عند أي تعارض بين الملفين، هذا الملف (OCR_CANONICAL) هو المرجع للقرارات المعمارية، وOCR_UNIFIED هو المرجع للتفاصيل التقنية.
