🔥 Master OCR Execution Plan (نسخة موحدة قابلة للتنفيذ)

هذا ليس “شرح”… هذا دستور تنفيذ + خطة عمل + أوامر تشغيل للوكيل
يجمع:

صرامة الخطة الأولى (Execution Blueprint) C:\Users\Bakheet\Documents\Projects\BG\OCR_HYBRID_EXTRACTION_PLAN.md
واقعية الخطة الثانية (Ensemble OCR) C:\Users\Bakheet\Documents\Projects\BG\OCR_ENSEMBLE_PLAN.md
🧠 أولاً: القاعدة الذهبية (لا يُكسر)

❗ أي كود لا يلتزم بهذه القاعدة يُرفض مباشرة

النظام = طبقتين منفصلتين
1) Orchestration (C# — النظام)
يتحكم بالـ pipeline
يخزن النتائج
يقرر UI + workflow
2) Extraction Engine (Python — worker)
يقرأ PDF
ينتج Candidates فقط
لا يقرر النهائي
🧱 ثانياً: العقد الأساسية (إجباري)
FieldCandidate (من الخطة الأولى)
public sealed class FieldCandidate
{
    public required string FieldName { get; init; }
    public required string Value { get; init; }
    public required double Confidence { get; init; }
    public required string Source { get; init; }

    public bool IsValid { get; set; }
    public string? Evidence { get; init; }
}
DocumentProcessingContext (قلب النظام)
public sealed class DocumentProcessingContext
{
    public string FileId { get; set; }

    public string RawDirectText { get; set; } = "";
    public string RawOcrText { get; set; } = "";

    public List<FieldCandidate> Candidates { get; } = new();
    public Dictionary<string, string> FinalFields { get; } = new();

    public List<string> Logs { get; } = new();
}

👆 هذا غير قابل للنقاش — هو ما يمنع الفوضى

⚙️ ثالثاً: الـ Pipeline النهائي (الموحد)
public async Task<DocumentProcessingContext> ProcessAsync(...)
{
    await ClassifyDocument();        // Bank + Type
    await ExtractDirectText();       // PyMuPDF
    await ExtractOcrText();          // PaddleOCR

    await ExtractCandidates();       // Regex + Bank Profiles
    await ValidateCandidates();      // Validation Graph
    await RunConsensus();            // Ensemble logic
    await ApplyConfidenceGate();     // Auto / Review

    return ctx;
}

👆 هذا يجمع الخطتين معًا:

الأولى: ترتيب صارم
الثانية: ensemble + consensus
🔥 رابعاً: أهم قرار معماري (لا يُكسر)
❗ ممنوع هذا:
Python يقرر FinalFields
Python يعمل Validation كامل
Python يعمل Gating
✅ المسموح:
Python يعطي:
Candidates
Confidence
Source
🧠 لماذا؟

لأن:

Python = استخراج فقط
C# = قرار

وهذا كان أكبر خطر في الخطة الثانية

⚡ خامساً: تطبيق Ensemble بدون تخريب
بدل:
if failed → OCR
يصبح:
تشغيل الاثنين → مقارنة → اختيار

لكن بطريقة مقيدة:

Python يرجع:
{
  "field": "amount",
  "value": "500000",
  "confidence": 0.82,
  "source": "native-text"
}

و:

{
  "field": "amount",
  "value": "500000",
  "confidence": 0.88,
  "source": "paddleocr"
}
🧠 C# يعمل Consensus (وليس Python)
var grouped = candidates.GroupBy(x => Normalize(x.Value));

var best = grouped
    .OrderByDescending(g => g.Sum(x => x.Confidence))
    .First();

👆 هذا أهم قرار في النظام كله

🧪 سادساً: Validation Graph (من الخطة الثانية)

لكن ينفذ هنا 👇

if (expiryDate <= issueDate)
    reject("Invalid date logic");

وليس في Python ❌

🚦 سابعاً: Confidence Gate
if (confidence < threshold)
    markForReview();
🧩 ثامناً: Bank Profiles (مسموح لكن بشروط)
✅ في Python:
patterns
regex
hints
❌ ممنوع:
اختيار القيمة النهائية
override النتائج
📊 تاسعاً: Feedback Loop (إجباري)
public class OcrFeedbackRecord
{
    public string Field;
    public string Extracted;
    public string Corrected;
    public string Bank;
}

👆 هذا ما سيحوّل النظام من “ذكي” إلى “يتعلم”

🧠 عاشراً: طريقة تشغيل AI (الأهم)
❌ لا تفعل:

"نفّذ النظام كامل"

✅ افعل:
المرحلة 1
أنشئ Models فقط
المرحلة 2
أنشئ Pipeline فقط (بدون منطق)
المرحلة 3
نفّذ ExtractCandidates فقط
المرحلة 4
أضف Validation
المرحلة 5
أضف Consensus
المرحلة 6
أضف UI
🧨 لماذا هذا مهم؟

لأنك سابقًا:

تعطيه خطة كاملة → ينفجر

الآن:

تعطيه جزء صغير → ينجح
🎯 الفرق النهائي بعد الدمج
قبل:
OCR بسيط
Regex
بدون تحقق
بدون تعلم
بعد:
Multi-source extraction
Consensus logic
Validation graph
Confidence gating
Feedback learning
💣 أهم 3 أشياء لا تفرّط فيها
DocumentProcessingContext
Pipeline Order
Responsibility Separation

إذا كُسر واحد منها → النظام يرجع فوضى

🚀 النتيجة المتوقعة (واقعيًا)

إذا طبقت هذا:

خلال أسبوعين:
دقة +15% إلى +25%
خلال شهر:
تقليل المراجعة البشرية بشكل واضح
خلال 3 أشهر:
نظام يتعلم من نفسه
