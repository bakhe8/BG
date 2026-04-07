# خطة OCR الموحدة والنهائية — نظام BG
## مستشفى الملك فيصل التخصصي ومركز الأبحاث

**الإصدار:** 1.0 — الخطة النهائية المعتمدة  
**التاريخ:** 2026-04-05  
**يحل محل:** OCR_ENSEMBLE_PLAN.md / OCR_HYBRID_EXTRACTION_PLAN.md / Master_OCR_Execution_Plan.md

---

## القسم الأول: الوضع الحالي (الحقيقة الكاملة)

### 1.1 ما يعمل الآن فعلياً في الكود

```
[Razor Page] → [QueuedOcrProcessingService] → [LocalPythonOcrProcessingService]
    → subprocess: python ocr_worker.py --request {temp_json}
    → stdout: JSON → OcrDocumentProcessingResult
```

**داخل `ocr_worker.py` — خط `process_scanned()`:**
```
لكل صفحة:
  if page_text >= 20 chars:
      build_structured_fields(text, "direct-pdf-text")   ← confidence مرتفعة
  else:
      render_page() → preprocess_image() → detect_layout_regions()
      run_ocr() per region → build_page_text()
      build_structured_fields(ocr_text, "paddleocr")     ← confidence منخفضة
```

**`make_field()` الحالية — ما يُرسَل لـ C#:**
```json
{
  "fieldKey": "IntakeField_GuaranteeNumber",
  "value": "23OGTE48803516",
  "confidencePercent": 99,
  "pageNumber": 1,
  "boundingBox": "auto",
  "sourceLabel": "direct-pdf-text"
}
```

**`OcrDocumentFieldCandidateDto` الحالي:**
```csharp
record OcrDocumentFieldCandidateDto(
    string FieldKey,
    string Value,
    int ConfidencePercent,
    int PageNumber,
    string? BoundingBox,
    string? SourceLabel);
```

### 1.2 ماذا تعني الأرقام الحالية؟

| الحقل | Confidence عند نص مباشر | Confidence عند OCR |
|-------|------------------------|-------------------|
| GuaranteeNumber | 99 | 92 |
| BankName (موجود في نص) | 95 | 88 |
| BankName (من payload فقط) | 65 | 65 |
| BankReference | 94 | 86 |
| IssueDate / OfficialDate | 85 | 78 |
| ExpiryDate / NewExpiryDate | 82 | 76 |
| Amount | 84 | 80 |
| CurrencyCode | 83 | 78 |
| Beneficiary | 78 | 70 |
| Principal | 76 | 68 |
| GuaranteeCategory | 72 | 66 |

**هذه الأرقام حالياً: ثابتة ومُقدَّرة يدوياً — لا تعكس دقة حقيقية مقاسة.**

### 1.3 نقاط الضعف الجوهرية الموثقة

1. **مصدر واحد فقط:** صفحة إما نص مباشر أو OCR، لا يُقارَن بينهما أبداً
2. **Confidence ثابتة:** 99 لنص مباشر حتى لو استُخرجت من سياق خاطئ
3. **لا تحقق متقاطع:** تاريخ الانتهاء قد يكون قبل تاريخ الإصدار ويُقبَل
4. **لا قرار Gating:** C# لا تعرف هل تحفظ آلياً أم تطلب مراجعة
5. **لا بيانات تقييم:** لا يوجد قياس فعلي للدقة لأي حقل لأي بنك

---

## القسم الثاني: القاعدة الذهبية (لا تُكسر أبداً)

```
┌─────────────────────────────────────────────────────────────────┐
│  Python Worker = استخراج فقط                                    │
│  يُنتج: Candidates (قيمة + ثقة + مصدر)                         │
│  ممنوع: اختيار القيمة النهائية / Validation / Gating           │
│                                                                   │
│  C# Orchestration = كل قرار                                     │
│  تقرر: Consensus / Validation / Gating / ما يُعرَض في UI       │
│  ممنوع: تطبيق regex / تشغيل OCR / تعديل النص الخام            │
└─────────────────────────────────────────────────────────────────┘
```

**لماذا هذا الحد صارم؟**  
لأن Python process مؤقت يموت بعد كل مستند — لا يعرف سياق الطلب، لا يعرف حالة النظام، لا يعرف سياسات القبول. C# هي الجهة الوحيدة التي تملك هذا السياق.

---

## القسم الثالث: عقد JSON بين Python وC# (الجديد)

### 3.1 المشكلة الحالية

Python ترسل حقلاً واحداً لكل field_key (بعد deduplication).  
النظام الجديد يحتاج **جميع المرشحين** لكل حقل حتى تستطيع C# مقارنتهم.

### 3.2 العقد الجديد (Python → C#)

**كل candidate يُرسَل بشكل منفصل — لا deduplication في Python:**

```json
{
  "succeeded": true,
  "processorName": "bg-python-ocr",
  "pipelineVersion": "wave3-multi-candidate",
  "candidates": [
    {
      "fieldKey": "IntakeField_GuaranteeNumber",
      "value": "23OGTE48803516",
      "confidencePercent": 92,
      "pageNumber": 1,
      "boundingBox": "120,45,680,90",
      "sourceLabel": "direct-pdf-text",
      "rawValue": "23OGTE48803516"
    },
    {
      "fieldKey": "IntakeField_GuaranteeNumber",
      "value": "23OGTE48803516",
      "confidencePercent": 85,
      "pageNumber": 1,
      "boundingBox": "120,45,680,90",
      "sourceLabel": "paddleocr"
    },
    {
      "fieldKey": "IntakeField_Amount",
      "value": "500000",
      "confidencePercent": 84,
      "pageNumber": 1,
      "boundingBox": "200,300,500,340",
      "sourceLabel": "direct-pdf-text",
      "rawValue": "500,000.00"
    },
    {
      "fieldKey": "IntakeField_Amount",
      "value": "500000",
      "confidencePercent": 80,
      "pageNumber": 1,
      "boundingBox": "200,300,500,340",
      "sourceLabel": "paddleocr",
      "rawValue": "500.000"
    }
  ],
  "warnings": [],
  "errorCode": null,
  "errorMessage": null,
  "detectedBankName": "Al Rajhi Bank",
  "pageClassifications": [
    { "pageNumber": 1, "type": "text-native", "charCount": 1240 },
    { "pageNumber": 2, "type": "image-only", "charCount": 3 }
  ]
}
```

**`rawValue`:** القيمة قبل التطبيع — تُحفَظ وتُعرَض للمراجع البشري فقط، لا تُستخدَم في الـ Consensus.

### 3.3 تغيير Python: إرسال candidates من كل مصدر

**التغيير الجوهري في `process_scanned()` و `process_text_first()`:**

```python
# الحالي: يختار مصدراً واحداً
# الجديد: يشغّل المصدرين ويجمعهم

def process_all_sources(document, request_payload, page_numbers, file_path):
    all_candidates = []
    
    for page_number in page_numbers:
        page = document[page_number - 1]
        native_text = (page.get_text("text") or "").strip()
        char_count = len(native_text)
        
        # Source A: النص المباشر — دائماً إذا وُجد
        if char_count >= 20:
            fields_a = build_structured_fields(
                native_text, request_payload, page_number, "direct-pdf-text"
            )
            all_candidates.extend(fields_a)
        
        # Source B: PaddleOCR
        # يعمل إذا: الصفحة image-only (إلزامي) أو mixed (للتحقق على الحقول الحرجة)
        should_run_ocr = (char_count < 20) or (20 <= char_count < 100)
        
        if should_run_ocr:
            rendered_path, render_warnings = render_page(file_path, page, page_number)
            if rendered_path:
                preprocessed_path, pre_warnings = preprocess_image(rendered_path)
                regions, layout_warnings = detect_layout_regions(preprocessed_path)
                ocr_results, ocr_warnings = run_ocr(preprocessed_path, regions or None)
                
                if ocr_results:
                    ocr_text = build_page_text(ocr_results)
                    fields_b = build_structured_fields(
                        ocr_text, request_payload, page_number, "paddleocr"
                    )
                    all_candidates.extend(fields_b)
    
    # لا deduplication هنا — C# تتولى ذلك
    return all_candidates
```

**ملاحظة الأداء:**  
OCR يعمل فقط على صفحات `image-only` أو `mixed` (< 100 حرف).  
صفحات `text-native` (>= 100 حرف): Source A فقط — لا OCR إضافي.  
هذا يحافظ على الأداء بينما يضيف التحقق للحالات المشكوك فيها.

---

## القسم الرابع: عقود البيانات في C#

### 4.1 FieldCandidate — المرشح الواحد (من Python)

```csharp
// BG.Application/Extraction/Contracts/FieldCandidate.cs

public enum CandidateSource
{
    DirectPdfText,   // PyMuPDF native text
    PaddleOcr,       // PaddleOCR pipeline
    LlmVision        // مستقبلي — Ollama local
}

public sealed class FieldCandidate
{
    public required string FieldKey { get; init; }
    public required string Value { get; init; }          // القيمة المُطبَّعة
    public required string RawValue { get; init; }       // القيمة الأصلية قبل التطبيع
    public required double Confidence { get; init; }     // 0.0 → 1.0
    public required CandidateSource Source { get; init; }
    public required int PageNumber { get; init; }
    public string? BoundingBox { get; init; }
    
    // تُعبأ بواسطة Validation Layer لاحقاً
    public bool IsValid { get; set; } = true;
    public string? ValidationMessage { get; set; }
}
```

### 4.2 FinalFieldResult — القرار النهائي لكل حقل

```csharp
// BG.Application/Extraction/Contracts/FinalFieldResult.cs

public enum FieldReviewDecision
{
    AutoAccepted,        // ثقة عالية، تحقق ناجح، مصادر متفقة
    ReviewRecommended,   // ثقة متوسطة أو تحذير validation
    ReviewRequired       // ثقة منخفضة أو خطأ validation أو مصادر مختلفة
}

public sealed class FinalFieldResult
{
    public required string FieldKey { get; init; }
    public string? Value { get; init; }               // null إذا لم يُستخرج شيء موثوق
    public string? RawValue { get; init; }            // للعرض في UI
    public required double Confidence { get; init; }  // الثقة النهائية بعد الـ consensus
    public required CandidateSource? WinningSource { get; init; }
    public required FieldReviewDecision ReviewDecision { get; init; }
    public string? ReviewReason { get; init; }         // سبب طلب المراجعة
    public required bool SourcesAgreed { get; init; } // هل اتفق المصدران؟
    public string? SourcesConflictDetail { get; init; } // تفاصيل الخلاف إن وجد
    public IReadOnlyList<FieldCandidate> AllCandidates { get; init; } = [];
}
```

### 4.3 DocumentProcessingContext — الحالة المشتركة بين المراحل

```csharp
// BG.Application/Extraction/Contracts/DocumentProcessingContext.cs

public sealed class DocumentProcessingContext
{
    public required string FileId { get; init; }
    public required string ScenarioKey { get; init; }
    public required string BankProfileKey { get; init; }
    public string? CanonicalBankName { get; init; }
    
    // يُعبأ بواسطة DocumentClassifier
    public string? DetectedBankName { get; set; }
    public IReadOnlyList<PageClassification> PageClassifications { get; set; } = [];
    
    // يُعبأ بواسطة CandidateExtractor (من Python)
    public List<FieldCandidate> Candidates { get; } = new();
    
    // يُعبأ بواسطة CandidateFusion
    public Dictionary<string, FinalFieldResult> FinalFields { get; }
        = new(StringComparer.Ordinal);
    
    // يُعبأ بواسطة ValidationLayer
    public List<ValidationIssue> ValidationIssues { get; } = new();
    
    // القرار النهائي — يُعبأ بواسطة ConfidenceGate
    public GatingDecision GatingDecision { get; set; } = GatingDecision.Unknown;
    public string? GatingReason { get; set; }
    
    // للتشخيص
    public List<string> ProcessingLog { get; } = new();
    public List<string> Warnings { get; } = new();
}

public sealed record PageClassification(int PageNumber, string Type, int CharCount);
    // Type: "text-native" | "mixed" | "image-only"

public sealed record ValidationIssue(
    string Severity,    // "error" | "warning"
    string FieldKey,
    string Message,
    string Code);

public enum GatingDecision
{
    Unknown,
    AutoAccepted,
    ReviewRecommended,
    ReviewRequired
}
```

---

## القسم الخامس: ترتيب التنفيذ (Pipeline) — إلزامي لا يُكسر

```csharp
// BG.Application/Extraction/Pipeline/DocumentProcessingPipeline.cs

public sealed class DocumentProcessingPipeline
{
    // الحقن عبر DI
    private readonly IDocumentClassifier _classifier;
    private readonly ICandidateExtractor _candidateExtractor;
    private readonly ICandidateValidator _validator;
    private readonly ICandidateFusion _fusion;
    private readonly IConfidenceGate _gate;
    private readonly ILogger<DocumentProcessingPipeline> _logger;

    public async Task<DocumentProcessingContext> ProcessAsync(
        DocumentProcessingInput input,
        CancellationToken ct)
    {
        var ctx = new DocumentProcessingContext
        {
            FileId = input.FileId,
            ScenarioKey = input.ScenarioKey,
            BankProfileKey = input.BankProfileKey,
            CanonicalBankName = input.CanonicalBankName
        };

        // Step 1: تصنيف المستند وتحديد البنك
        await _classifier.ClassifyAsync(ctx, input, ct);
        _logger.LogInformation("Step 1 complete: Bank={Bank}, Pages={Pages}",
            ctx.DetectedBankName, ctx.PageClassifications.Count);

        // Step 2: استخراج الـ Candidates من Python
        await _candidateExtractor.ExtractAsync(ctx, input, ct);
        _logger.LogInformation("Step 2 complete: {Count} candidates extracted",
            ctx.Candidates.Count);

        // Step 3: التحقق من صحة كل Candidate منفرداً
        await _validator.ValidateAsync(ctx, ct);
        _logger.LogInformation("Step 3 complete: {Valid} valid, {Invalid} invalid",
            ctx.Candidates.Count(c => c.IsValid),
            ctx.Candidates.Count(c => !c.IsValid));

        // Step 4: الدمج والـ Consensus
        await _fusion.SelectBestAsync(ctx, ct);
        _logger.LogInformation("Step 4 complete: {Count} final fields resolved",
            ctx.FinalFields.Count);

        // Step 5: قرار الـ Gating (حفظ آلي / مراجعة)
        await _gate.ApplyAsync(ctx, ct);
        _logger.LogInformation("Step 5 complete: Decision={Decision}, Reason={Reason}",
            ctx.GatingDecision, ctx.GatingReason);

        return ctx;
    }
}
```

### الـ Interfaces المطلوبة

```csharp
public interface IDocumentClassifier
{
    Task ClassifyAsync(DocumentProcessingContext ctx, DocumentProcessingInput input, CancellationToken ct);
}

public interface ICandidateExtractor
{
    // يستدعي Python worker ويحوّل الـ JSON إلى قائمة FieldCandidate
    Task ExtractAsync(DocumentProcessingContext ctx, DocumentProcessingInput input, CancellationToken ct);
}

public interface ICandidateValidator
{
    // يتحقق من كل Candidate منفرداً ويضع IsValid/ValidationMessage
    Task ValidateAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface ICandidateFusion
{
    // يقارن الـ candidates لكل field_key ويختار الأفضل
    Task SelectBestAsync(DocumentProcessingContext ctx, CancellationToken ct);
}

public interface IConfidenceGate
{
    // يطبق سياسات القبول الآلي ويضع GatingDecision
    Task ApplyAsync(DocumentProcessingContext ctx, CancellationToken ct);
}
```

---

## القسم السادس: Consensus Engine — منطق الدمج (C# فقط)

```csharp
// BG.Application/Extraction/Stages/CandidateFusion.cs

public sealed class CandidateFusion : ICandidateFusion
{
    public Task SelectBestAsync(DocumentProcessingContext ctx, CancellationToken ct)
    {
        // جمع الـ candidates المقبولة فقط
        var validCandidates = ctx.Candidates
            .Where(c => c.IsValid)
            .GroupBy(c => c.FieldKey);

        foreach (var group in validCandidates)
        {
            var result = ComputeConsensus(group.Key, group.ToList());
            ctx.FinalFields[group.Key] = result;
        }

        return Task.CompletedTask;
    }

    private static FinalFieldResult ComputeConsensus(
        string fieldKey,
        List<FieldCandidate> candidates)
    {
        if (candidates.Count == 0)
            return BuildEmpty(fieldKey, "no-valid-candidates");

        if (candidates.Count == 1)
        {
            var single = candidates[0];
            return new FinalFieldResult
            {
                FieldKey = fieldKey,
                Value = single.Value,
                RawValue = single.RawValue,
                Confidence = single.Confidence * 0.80, // خصم 20% — مصدر واحد غير مُتحقَّق
                WinningSource = single.Source,
                ReviewDecision = single.Confidence >= 0.85
                    ? FieldReviewDecision.ReviewRecommended
                    : FieldReviewDecision.ReviewRequired,
                ReviewReason = "single-source-only",
                SourcesAgreed = false,
                AllCandidates = candidates
            };
        }

        // تطبيع القيم للمقارنة فقط — القيمة الأصلية محفوظة في RawValue
        var normalizedGroups = candidates
            .GroupBy(c => NormalizeForComparison(fieldKey, c.Value))
            .ToList();

        var majorityGroup = normalizedGroups
            .OrderByDescending(g => g.Sum(c => c.Confidence))
            .First();

        var totalConfidence = candidates.Sum(c => c.Confidence);
        var majorityConfidence = majorityGroup.Sum(c => c.Confidence);
        var agreementRatio = majorityConfidence / totalConfidence;

        var winner = majorityGroup
            .OrderByDescending(c => c.Confidence)
            .First();

        double finalConfidence;
        FieldReviewDecision decision;
        string? reason;
        bool agreed;

        if (agreementRatio >= 0.80)
        {
            // اتفاق قوي بين المصادر
            finalConfidence = Math.Min(0.97, majorityConfidence / majorityGroup.Count() * 1.15);
            decision = FieldReviewDecision.AutoAccepted;
            reason = null;
            agreed = true;
        }
        else if (agreementRatio >= 0.60)
        {
            // اتفاق جزئي
            finalConfidence = majorityConfidence / majorityGroup.Count();
            decision = finalConfidence >= 0.75
                ? FieldReviewDecision.ReviewRecommended
                : FieldReviewDecision.ReviewRequired;
            reason = "partial-agreement";
            agreed = false;
        }
        else
        {
            // خلاف حقيقي
            finalConfidence = winner.Confidence * 0.50;
            decision = FieldReviewDecision.ReviewRequired;
            var conflictValues = candidates.Select(c => $"{c.Source}: {c.Value}");
            reason = $"sources-conflict: {string.Join(" | ", conflictValues)}";
            agreed = false;
        }

        return new FinalFieldResult
        {
            FieldKey = fieldKey,
            Value = winner.Value,
            RawValue = winner.RawValue,
            Confidence = finalConfidence,
            WinningSource = winner.Source,
            ReviewDecision = decision,
            ReviewReason = reason,
            SourcesAgreed = agreed,
            SourcesConflictDetail = agreed ? null : reason,
            AllCandidates = candidates
        };
    }

    private static string NormalizeForComparison(string fieldKey, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        // تحويل الأرقام العربية للغربية
        var v = value
            .Replace("٠", "0").Replace("١", "1").Replace("٢", "2")
            .Replace("٣", "3").Replace("٤", "4").Replace("٥", "5")
            .Replace("٦", "6").Replace("٧", "7").Replace("٨", "8").Replace("٩", "9");

        return fieldKey switch
        {
            "IntakeField_Amount" =>
                // 500,000.00 == 500000 == 500،000
                System.Text.RegularExpressions.Regex.Replace(v, @"[,،\s]", "")
                    .TrimEnd('0').TrimEnd('.'),

            "IntakeField_IssueDate" or "IntakeField_ExpiryDate"
                or "IntakeField_NewExpiryDate" or "IntakeField_OfficialLetterDate" =>
                // التواريخ يُفترَض أن Python طبّعتها لـ yyyy-mm-dd بالفعل
                // هنا نتحقق فقط من التطابق المباشر
                v.Trim(),

            "IntakeField_GuaranteeNumber" =>
                // BG-2024-001 == BG2024001 — أزل الفواصل والشرطات
                System.Text.RegularExpressions.Regex.Replace(v, @"[\s\-/]", "").ToUpperInvariant(),

            "IntakeField_BankName" =>
                // تطبيع بسيط لأسماء البنوك
                v.Trim().ToLowerInvariant(),

            _ => v.Trim().ToLowerInvariant()
        };
    }

    private static FinalFieldResult BuildEmpty(string fieldKey, string reason) =>
        new()
        {
            FieldKey = fieldKey,
            Value = null,
            RawValue = null,
            Confidence = 0.0,
            WinningSource = null,
            ReviewDecision = FieldReviewDecision.ReviewRequired,
            ReviewReason = reason,
            SourcesAgreed = false,
            AllCandidates = []
        };
}
```

---

## القسم السابع: Validation Graph — التحقق المتقاطع (C# فقط)

```csharp
// BG.Application/Extraction/Stages/CandidateValidator.cs

public sealed class CandidateValidator : ICandidateValidator
{
    // يُطبَّق على كل Candidate منفرداً قبل الـ Consensus
    public Task ValidateAsync(DocumentProcessingContext ctx, CancellationToken ct)
    {
        foreach (var candidate in ctx.Candidates)
        {
            ValidateCandidate(candidate);
        }
        return Task.CompletedTask;
    }

    private static void ValidateCandidate(FieldCandidate candidate)
    {
        switch (candidate.FieldKey)
        {
            case "IntakeField_GuaranteeNumber":
                ValidateGuaranteeNumber(candidate);
                break;
            case "IntakeField_Amount":
                ValidateAmount(candidate);
                break;
            case "IntakeField_IssueDate":
            case "IntakeField_ExpiryDate":
            case "IntakeField_NewExpiryDate":
            case "IntakeField_OfficialLetterDate":
                ValidateDate(candidate);
                break;
            case "IntakeField_CurrencyCode":
                ValidateCurrency(candidate);
                break;
        }
    }

    private static void ValidateGuaranteeNumber(FieldCandidate c)
    {
        var v = c.Value?.Trim() ?? string.Empty;

        // رقم السجل التجاري السعودي: 10 أرقام تبدأ بـ 1 أو 2
        if (System.Text.RegularExpressions.Regex.IsMatch(v, @"^[12]\d{9}$"))
        {
            c.IsValid = false;
            c.ValidationMessage = "يشبه رقم السجل التجاري السعودي";
            return;
        }

        // أقل من 6 أحرف — ضعيف جداً
        if (v.Length < 6)
        {
            c.IsValid = false;
            c.ValidationMessage = "رقم الضمان قصير جداً";
        }
    }

    private static void ValidateAmount(FieldCandidate c)
    {
        if (!double.TryParse(c.Value, out var amount))
        {
            c.IsValid = false;
            c.ValidationMessage = "المبلغ ليس رقماً صالحاً";
            return;
        }

        if (amount < 1_000)
        {
            c.IsValid = false;
            c.ValidationMessage = $"المبلغ {amount:N0} منخفض جداً لضمان بنكي";
        }

        if (amount > 500_000_000)
        {
            // لا نرفض — نحذّر فقط
            c.ValidationMessage = "مبلغ كبير جداً — يتطلب مراجعة متأنية";
        }
    }

    private static void ValidateDate(FieldCandidate c)
    {
        if (!DateOnly.TryParseExact(c.Value, "yyyy-MM-dd", out _))
        {
            c.IsValid = false;
            c.ValidationMessage = "صيغة التاريخ غير صالحة (المتوقع: yyyy-MM-dd)";
        }
    }

    private static void ValidateCurrency(FieldCandidate c)
    {
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "SAR", "USD", "EUR", "GBP" };

        if (!supported.Contains(c.Value ?? string.Empty))
        {
            c.IsValid = false;
            c.ValidationMessage = $"رمز العملة '{c.Value}' غير مدعوم";
        }
    }
}
```

### التحقق المتقاطع (بعد الـ Consensus) — يُضاف إلى ConfidenceGate

```csharp
// يُنفَّذ داخل ConfidenceGate.ApplyAsync بعد FinalFields مكتملة

private static void RunCrossFieldValidation(DocumentProcessingContext ctx)
{
    var issueDate = GetDateField(ctx, "IntakeField_IssueDate");
    var expiryDate = GetDateField(ctx, "IntakeField_ExpiryDate");
    var newExpiryDate = GetDateField(ctx, "IntakeField_NewExpiryDate");

    // قاعدة 1: تاريخ الانتهاء بعد تاريخ الإصدار
    if (issueDate.HasValue && expiryDate.HasValue && expiryDate <= issueDate)
    {
        ctx.ValidationIssues.Add(new ValidationIssue(
            "error",
            "IntakeField_ExpiryDate",
            "تاريخ الانتهاء يجب أن يكون بعد تاريخ الإصدار",
            "date.expiry-before-issue"));
    }

    // قاعدة 2: مدة الضمان منطقية (30 يوم → 10 سنوات)
    if (issueDate.HasValue && expiryDate.HasValue)
    {
        var days = (expiryDate.Value.ToDateTime(TimeOnly.MinValue) -
                    issueDate.Value.ToDateTime(TimeOnly.MinValue)).Days;

        if (days < 30)
            ctx.ValidationIssues.Add(new ValidationIssue(
                "warning", "IntakeField_ExpiryDate",
                $"مدة الضمان {days} يوم فقط — تحقق", "date.duration-too-short"));

        if (days > 3650)
            ctx.ValidationIssues.Add(new ValidationIssue(
                "warning", "IntakeField_ExpiryDate",
                $"مدة الضمان {days / 365} سنة — تحقق", "date.duration-too-long"));
    }

    // قاعدة 3: للتمديد — التاريخ الجديد بعد الأصلي
    if (expiryDate.HasValue && newExpiryDate.HasValue && newExpiryDate <= expiryDate)
    {
        ctx.ValidationIssues.Add(new ValidationIssue(
            "error", "IntakeField_NewExpiryDate",
            "تاريخ الانتهاء الجديد يجب أن يكون بعد الأصلي",
            "date.new-expiry-before-original"));
    }
}

private static DateOnly? GetDateField(DocumentProcessingContext ctx, string key)
{
    if (!ctx.FinalFields.TryGetValue(key, out var field) || field.Value is null)
        return null;
    return DateOnly.TryParseExact(field.Value, "yyyy-MM-dd", out var d) ? d : null;
}
```

---

## القسم الثامن: Confidence Gate — قرار القبول أو المراجعة

```csharp
// BG.Application/Extraction/Stages/ConfidenceGate.cs

public sealed class ConfidenceGate : IConfidenceGate
{
    // عتبات القبول الآلي لكل حقل حرج
    // هذه الأرقام تُعاد معايرتها بعد جمع Ground Truth حقيقي
    private static readonly Dictionary<string, double> AutoAcceptThresholds = new()
    {
        ["IntakeField_GuaranteeNumber"] = 0.88,
        ["IntakeField_Amount"]          = 0.85,
        ["IntakeField_ExpiryDate"]      = 0.82,
        ["IntakeField_IssueDate"]       = 0.82,
        ["IntakeField_BankName"]        = 0.80,
    };

    public Task ApplyAsync(DocumentProcessingContext ctx, CancellationToken ct)
    {
        // أولاً: التحقق المتقاطع بين الحقول
        RunCrossFieldValidation(ctx);

        // ثانياً: تطبيق عتبات الثقة
        ApplyThresholds(ctx);

        // ثالثاً: القرار النهائي للمستند كاملاً
        ctx.GatingDecision = DetermineGatingDecision(ctx);
        ctx.GatingReason = BuildGatingReason(ctx);

        return Task.CompletedTask;
    }

    private static void ApplyThresholds(DocumentProcessingContext ctx)
    {
        foreach (var (fieldKey, threshold) in AutoAcceptThresholds)
        {
            if (!ctx.FinalFields.TryGetValue(fieldKey, out var field))
                continue;

            if (field.Value is null)
            {
                // حقل حرج مفقود → مراجعة إلزامية
                ctx.ValidationIssues.Add(new ValidationIssue(
                    "error", fieldKey,
                    $"الحقل الحرج '{fieldKey}' لم يُستخرج",
                    "field.missing-critical"));
            }
            else if (field.Confidence < threshold)
            {
                // ثقة أقل من الحد → مراجعة إلزامية
                ctx.ValidationIssues.Add(new ValidationIssue(
                    "warning", fieldKey,
                    $"ثقة {field.Confidence:P0} أقل من الحد {threshold:P0}",
                    "field.low-confidence"));
            }
        }
    }

    private static GatingDecision DetermineGatingDecision(DocumentProcessingContext ctx)
    {
        // أي خطأ validation → مراجعة إلزامية
        if (ctx.ValidationIssues.Any(i => i.Severity == "error"))
            return GatingDecision.ReviewRequired;

        // أي حقل حرج يحتاج مراجعة
        var criticalFields = AutoAcceptThresholds.Keys;
        if (ctx.FinalFields.Values
            .Where(f => criticalFields.Contains(f.FieldKey))
            .Any(f => f.ReviewDecision == FieldReviewDecision.ReviewRequired))
            return GatingDecision.ReviewRequired;

        // تحذيرات أو توصية بالمراجعة
        if (ctx.ValidationIssues.Any(i => i.Severity == "warning"))
            return GatingDecision.ReviewRecommended;

        if (ctx.FinalFields.Values.Any(f => f.ReviewDecision == FieldReviewDecision.ReviewRecommended))
            return GatingDecision.ReviewRecommended;

        return GatingDecision.AutoAccepted;
    }
}
```

---

## القسم التاسع: معالجة حالات الفشل (Failure Modes)

### 9.1 جدول حالات الفشل والاستجابة

| الحالة | السبب | الاستجابة |
|--------|-------|-----------|
| Python لا يبدأ | PythonExecutablePath خاطئ أو Python غير مثبت | OcrDocumentProcessingResult.ErrorCode = "ocr.python_not_found" → يُعرَض للمستخدم |
| Python يتجاوز الـ Timeout | ملف ضخم أو OCR بطيء | TryKill() → ErrorCode = "ocr.timeout" → يُوضَع في ReviewRequired |
| PDF تالف | PyMuPDF يرمي استثناء | try/catch في extract_native_text → يُحوَّل لـ image-only مباشرةً |
| PaddleOCR لا يبدأ | مكتبة غير مثبتة | warning: "paddleocr-missing-or-init-failed" → يُكمل بـ native-text فقط |
| LayoutParser غير متوفر | مكتبة غير مثبتة | warning: "layoutparser-missing" → OCR على الصفحة كاملة |
| OOM في Python | ملف كبير جداً | exit code ≠ 0 → ErrorCode = "ocr.worker_failed" → ReviewRequired |
| Queue ممتلئ (32 طلب) | BoundedChannel.Wait | طلب جديد ينتظر أو يُرفَض بـ "ocr.queue_unavailable" |
| JSON غير صالح في stdout | خطأ في Python | TryDeserializeWorkerResult يمسح الأسطر الأخيرة → fallback |
| صفحات المستند = 0 | PDF فارغ | usable_page_count == 0 → ReviewRequired + "empty-document" |

### 9.2 استجابة C# الموحدة لأي فشل في Python

```csharp
// أي فشل من Python لا يوقف العملية — يضع ReviewRequired فقط
if (!pythonResult.Succeeded)
{
    ctx.GatingDecision = GatingDecision.ReviewRequired;
    ctx.GatingReason = $"فشل استخراج OCR: {pythonResult.ErrorCode}";
    ctx.Warnings.Add(pythonResult.ErrorMessage ?? "unknown OCR error");
    // لا exception — المستخدم يُراجع يدوياً
    return;
}
```

### 9.3 Timeout Policy

```
TimeoutSeconds الحالي: 90 ثانية
للملفات العادية (1-4 صفحات): كافٍ
للملفات الكبيرة (10+ صفحات): يرفع إلى 180 ثانية (يُضبَط في LocalOcrOptions)
```

---

## القسم العاشر: Ground Truth — الخطة الفعلية

### 10.1 الجواب على سؤال "من يفعل ذلك؟"

التوسيم يفعله **موظف الإدخال (Intake Operator)** أثناء عمله الطبيعي.  
النظام يُسجّل التصحيحات التي يجريها تلقائياً في جدول `OcrFeedbackRecords`.  
**لا يوجد عمل إضافي — الـ Ground Truth يُبنى من الإنتاج الحقيقي.**

### 10.2 ملف Ground Truth الأولي (للـ baseline فقط)

للحصول على baseline يسبق أي تغيير:

```
المطلوب: 30-50 مستند فقط (وليس 200-300)
التوزيع: 5-10 مستندات من كل بنك رئيسي (الراجحي، الأهلي، الرياض)
المُوسِّم: مشرف قسم الضمانات البنكية (يعرف القيم الصحيحة)
الوقت: 3-5 ساعات عمل (5 دقائق × 50 مستند)
```

```csv
# ocr_ground_truth_baseline.csv
document_id,bank_name,scenario,guarantee_number,amount,issue_date,expiry_date,currency,notes
DOC001,Al Rajhi Bank,new-guarantee,23OGTE48803516,500000,2024-01-15,2025-01-15,SAR,
DOC002,Saudi National Bank,new-guarantee,SNB2024001234,1200000,2024-02-01,2026-02-01,SAR,مبلغ بالحروف في الأصل
DOC003,Riyad Bank,extension-confirmation,,,,2025-06-30,,تمديد فقط تاريخ
...
```

### 10.3 سكريبت القياس الأساسي

```python
# scripts/evaluate_baseline.py
# يشغَّل مرة واحدة قبل التغييرات ومرة بعدها

def evaluate(ground_truth_csv, documents_folder, ocr_worker_script):
    results = []
    for row in load_csv(ground_truth_csv):
        raw = subprocess.run([python, ocr_worker_script, '--request', ...])
        extracted = parse_result(raw.stdout)
        
        for field in ['guarantee_number', 'amount', 'issue_date', 'expiry_date']:
            gt = normalize(field, row[field])
            ex = normalize(field, extracted.get(field))
            results.append({
                'bank': row['bank_name'], 'field': field,
                'match': gt == ex, 'confidence': extracted.get_confidence(field)
            })
    
    # تقرير: دقة كل حقل لكل بنك
    print_report(results)
```

**المخرج المتوقع من الـ baseline (مثال):**
```
الحقل            | الراجحي | الأهلي  | الرياض  | الإجمالي
GuaranteeNumber  |   82%   |   78%   |   85%   |   82%
Amount           |   88%   |   85%   |   90%   |   88%
ExpiryDate       |   75%   |   70%   |   80%   |   75%
IssueDate        |   78%   |   73%   |   82%   |   78%
```

---

## القسم الحادي عشر: Feedback Loop (الواقعي)

### 11.1 ما يُخزَّن

```csharp
// BG.Domain/Entities/OcrFeedbackRecord.cs

public sealed class OcrFeedbackRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid DocumentId { get; init; }
    public required string FieldKey { get; init; }
    public required string ExtractedValue { get; init; }
    public required string CorrectedValue { get; init; }
    public string? DetectedBankName { get; init; }
    public string? ScenarioKey { get; init; }
    public string? PipelineVersion { get; init; }
    public required int ConfidencePercent { get; init; }
    public required bool SourcesAgreed { get; init; }
    public required string WinningSource { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public required string RecordedByUserId { get; init; }
}
```

### 11.2 متى يُسجَّل

```csharp
// في Intake Review Page — عند حفظ الحقل بعد تعديل
if (submittedValue != extractedValue)
{
    await _feedbackService.RecordAsync(new OcrFeedbackRecord
    {
        DocumentId = documentId,
        FieldKey = fieldKey,
        ExtractedValue = extractedValue,
        CorrectedValue = submittedValue,
        DetectedBankName = ctx.DetectedBankName,
        ScenarioKey = ctx.ScenarioKey,
        PipelineVersion = pythonResult.PipelineVersion,
        ConfidencePercent = (int)(field.Confidence * 100),
        SourcesAgreed = field.SourcesAgreed,
        WinningSource = field.WinningSource?.ToString() ?? "unknown",
        RecordedAt = DateTimeOffset.UtcNow,
        RecordedByUserId = currentUserId
    });
}
```

### 11.3 كيف تُستخدَم الـ Feedback (الواقعي)

```
كل أسبوعين: تقرير تلقائي يُرسَل للمشرف التقني يحتوي:

"الأخطاء المتكررة هذا الأسبوع:"
- IntakeField_ExpiryDate في الراجحي: 8 تصحيحات
  المستخرج: 2025-01-15 / الصحيح: 2026-01-15 (سنة خاطئة)
  → السبب المحتمل: regex يلتقط التاريخ الأول في الصفحة لا الأخير
  → الإجراء: تعديل EXPIRY_DATE_CONTEXT_REGEXES لبنك الراجحي

- IntakeField_Amount في الأهلي: 5 تصحيحات
  المستخرج: 50000 / الصحيح: 500000 (فاصلة مفقودة)
  → السبب: OCR يقرأ 500.000 كـ 500.000 ويحذف الأصفار
  → الإجراء: تحسين normalize_amount للتعامل مع النقطة العشرية الأوروبية
```

**هذا قابل للتنفيذ** لأن التصحيح هو: مراجعة بشرية أسبوعية → تعديل regex محدد → قياس أثره.

---

## القسم الثاني عشر: واجهة المراجعة البشرية

### 12.1 ما يُعرَض لكل حقل

```
┌─────────────────────────────────────────────────────────────────┐
│  رقم الضمان                                                      │
│  ┌─────────────────────────────────────────────────────┐       │
│  │  23OGTE48803516                             [تعديل]  │       │
│  └─────────────────────────────────────────────────────┘       │
│                                                                   │
│  الثقة: ████████████ 94%  [أخضر]                               │
│  المصادر: PyMuPDF ✓  OCR ✓  (اتفق المصدران)                   │
│  الصفحة 1                                                        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  تاريخ الانتهاء                              ⚠️ يتطلب مراجعة  │
│  ┌─────────────────────────────────────────────────────┐       │
│  │  2025-01-15                                 [تعديل]  │       │
│  └─────────────────────────────────────────────────────┘       │
│                                                                   │
│  الثقة: ████░░░░░░░░ 45%  [أحمر]                               │
│  ⚠️ المصادر اختلفت:                                             │
│     PyMuPDF: 2025-01-15    OCR: 2026-01-15                     │
│  الصفحة 1                                                        │
└─────────────────────────────────────────────────────────────────┘
```

### 12.2 ما يُعرض للمراجع عند الخلاف

```csharp
// في Razor Page — عند SourcesAgreed == false
@if (!field.SourcesAgreed && field.AllCandidates.Count > 1)
{
    <div class="conflict-detail">
        <span>المصادر اختلفت:</span>
        @foreach (var candidate in field.AllCandidates)
        {
            <span>@candidate.Source: @candidate.RawValue 
                  (ثقة: @candidate.Confidence.ToString("P0"))</span>
        }
    </div>
}
```

---

## القسم الثالث عشر: قياس الأداء والـ Observability

### 13.1 Metrics المطلوبة

```csharp
// يُسجَّل بعد كل مستند
_logger.LogInformation(
    "OCR_COMPLETE: Bank={Bank} Scenario={Scenario} Decision={Decision} " +
    "DurationMs={Duration} CandidateCount={Candidates} " +
    "AgreedFields={Agreed} ConflictedFields={Conflicted} " +
    "ValidationErrors={Errors}",
    ctx.DetectedBankName, ctx.ScenarioKey, ctx.GatingDecision,
    stopwatch.ElapsedMilliseconds, ctx.Candidates.Count,
    ctx.FinalFields.Values.Count(f => f.SourcesAgreed),
    ctx.FinalFields.Values.Count(f => !f.SourcesAgreed),
    ctx.ValidationIssues.Count(i => i.Severity == "error"));
```

### 13.2 المقياس التشغيلي الرئيسي

```
المقياس الوحيد الذي يهم المستخدم:
نسبة المستندات التي وصلت GatingDecision = AutoAccepted دون تصحيح بشري

الهدف:
  الأسبوع 1 (baseline): تسجيل الرقم الحالي
  الشهر 1: تحسن واضح مقارنة بـ baseline
  الشهر 3: 70%+ auto-accepted دون تصحيح
```

---

## القسم الرابع عشر: هيكل الملفات (Folder Structure)

```
src/
├── BG.Application/
│   └── Extraction/
│       ├── Contracts/
│       │   ├── FieldCandidate.cs
│       │   ├── FinalFieldResult.cs
│       │   ├── DocumentProcessingContext.cs
│       │   └── Interfaces/
│       │       ├── IDocumentClassifier.cs
│       │       ├── ICandidateExtractor.cs
│       │       ├── ICandidateValidator.cs
│       │       ├── ICandidateFusion.cs
│       │       └── IConfidenceGate.cs
│       ├── Pipeline/
│       │   └── DocumentProcessingPipeline.cs
│       └── Stages/
│           ├── CandidateValidator.cs       ← قواعد فردية لكل حقل
│           ├── CandidateFusion.cs          ← Consensus Engine
│           └── ConfidenceGate.cs           ← Gating + Cross-field validation
│
├── BG.Domain/
│   └── Entities/
│       └── OcrFeedbackRecord.cs
│
├── BG.Infrastructure/
│   └── Feedback/
│       └── OcrFeedbackService.cs
│
└── BG.Integrations/
    └── OcrWorker/
        └── ocr_worker.py                   ← تغييرات: multi-candidate output
```

---

## القسم الخامس عشر: A/B Testing — كيف نُثبت أن الجديد أفضل

### قبل أي نشر

1. شغّل `evaluate_baseline.py` على الـ 50 مستند → احفظ النتائج
2. طبّق التغييرات (Pipeline الجديد)
3. شغّل `evaluate_baseline.py` مجدداً على نفس المستندات
4. قارن: هل الدقة ارتفعت؟ هل وقت المعالجة قبِل؟

### في الإنتاج

```
أسبوع 1 بعد النشر:
  - راقب: GatingDecision distribution (كم % AutoAccepted vs ReviewRequired)
  - راقب: متوسط وقت المعالجة (P50 / P95)
  - راقب: OcrFeedbackRecords التي تُضاف (كم تصحيح يومياً)

إذا زادت الـ ReviewRequired عن baseline → تراجع عن التغيير الأخير
إذا ارتفعت الـ AutoAccepted → استمر
```

---

## القسم السادس عشر: خطة التنفيذ المرحلية

### المرحلة صفر — Baseline (3 أيام)

**الهدف:** رقم واضح للوضع الحالي قبل أي تغيير

- [ ] اختر 30-50 مستند حقيقي من الأرشيف (10+ من كل بنك رئيسي)
- [ ] مشرف القسم يُدخل القيم الصحيحة في `ocr_ground_truth_baseline.csv`
- [ ] شغّل `evaluate_baseline.py` → احفظ النتائج
- [ ] سجّل: متوسط وقت المعالجة الحالي

**مخرج:** "دقة الاستخراج الحالية X% للضمان، Y% للمبلغ، Z% للتواريخ"

---

### المرحلة الأولى — Data Contracts + Pipeline Skeleton (أسبوع 1)

**لا تلمس `ocr_worker.py` في هذه المرحلة**

- [ ] `FieldCandidate.cs` — القسم 4.1
- [ ] `FinalFieldResult.cs` — القسم 4.2
- [ ] `DocumentProcessingContext.cs` — القسم 4.3
- [ ] Interfaces — القسم 5
- [ ] `DocumentProcessingPipeline.cs` — القسم 5 (skeleton فارغ)
- [ ] Unit tests: pipeline يمر بالخطوات الصحيحة بالترتيب

**اختبار:** `dotnet build` + `dotnet test` يجب أن ينجحا

---

### المرحلة الثانية — تعديل Python لـ Multi-Candidate (أسبوع 2)

- [ ] تعديل `process_all_sources()` — القسم 3.3
- [ ] تعديل `canonical_result()` لإرجاع `candidates` بدل `fields`
- [ ] إضافة `pageClassifications` للـ JSON output
- [ ] تعديل `ICandidateExtractor` implementation لقراءة الصيغة الجديدة
- [ ] اختبار: تشغيل على مستند واحد من الـ Ground Truth → التحقق من JSON

---

### المرحلة الثالثة — CandidateValidator + CandidateFusion (أسبوع 3)

- [ ] `CandidateValidator.cs` — القسم 7
- [ ] `CandidateFusion.cs` (Consensus Engine) — القسم 6
- [ ] `RunCrossFieldValidation()` — القسم 7
- [ ] Unit tests: حالات الاتفاق، الخلاف، مصدر واحد
- [ ] شغّل `evaluate_baseline.py` مجدداً → قارن مع baseline

---

### المرحلة الرابعة — ConfidenceGate + UI + Feedback (أسبوع 4)

- [ ] `ConfidenceGate.cs` — القسم 8
- [ ] تعديل Razor Page لعرض المعلومات الجديدة — القسم 12
- [ ] `OcrFeedbackRecord.cs` + Migration + `OcrFeedbackService.cs` — القسم 11
- [ ] ربط التصحيحات بـ `RecordAsync()` في الـ Intake page
- [ ] اختبار نهاية لنهاية: مستند حقيقي من الاستقبال للحفظ

---

### المرحلة الخامسة — Observability + A/B (أسبوع 5)

- [ ] إضافة `LogInformation` لكل مستند — القسم 13
- [ ] تقرير أسبوعي أوتوماتيكي من `OcrFeedbackRecords`
- [ ] قياس A/B: baseline vs الجديد — القسم 15

---

## القسم السابع عشر: ما هو خارج نطاق هذه الخطة

| الميزة | السبب |
|--------|-------|
| LLM (Ollama/Qwen2-VL) | يتطلب GPU أو موارد إضافية، يُضاف فقط إذا أثبتت المرحلة 5 أن الخلاف المتبقي يستحق التكلفة |
| Bank Profiles تلقائية | تُبنى يدوياً من تحليل الـ Feedback بعد شهر من الإنتاج |
| نموذج ML مخصص | يتطلب Ground Truth ضخم (1000+ مستند) — مرحلة مستقبلية |
| gRPC بدل subprocess | الـ subprocess الحالي يعمل ولا يوجد سبب لتغييره الآن |
| تحديث تلقائي للـ regex | خطر التدهور — التحديث يدوي دائماً بعد مراجعة بشرية |

---

## ملخص الفروق: قبل وبعد

| الجانب | الحالي | بعد التنفيذ |
|--------|--------|-------------|
| مصادر الاستخراج | 1 (إما/أو) | 1-2 بالتوازي حسب نوع الصفحة |
| Confidence | ثابتة ومُقدَّرة | محسوبة من اتفاق المصادر |
| Validation | غير موجود | فردي + متقاطع |
| Gating | غير موجود | 3 مستويات واضحة |
| عرض الثقة في UI | غير موجود | شريط + مصادر + تفاصيل خلاف |
| Feedback | غير موجود | جدول DB + تقرير أسبوعي |
| قياس الدقة | غير موجود | Ground Truth + baseline + مقارنة |
| Failure handling | جزئي | شامل لكل حالة |
| مكان القرار | Python | C# حصراً |

---

*هذا الملف هو المرجع الوحيد للتنفيذ — يُحدَّث بعد كل مرحلة بالنتائج الفعلية.*  
*الملفات القديمة: OCR_ENSEMBLE_PLAN.md / OCR_HYBRID_EXTRACTION_PLAN.md / Master_OCR_Execution_Plan.md — مرجعية فقط.*
