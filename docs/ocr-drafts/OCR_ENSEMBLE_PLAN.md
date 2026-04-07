# خطة تطوير OCR: Ensemble Extraction Architecture
## نظام BG — مستشفى الملك فيصل التخصصي

**تاريخ الخطة:** 2026-04-05  
**الحالة:** خطة تنفيذ تفصيلية — لم تبدأ بعد

---

## القسم الأول: الوضع الحالي (كما هو في الكود)

### 1.1 البنية المعمارية الحالية

```
[المستخدم يرفع PDF]
        ↓
[BG.Web — Razor Page]
        ↓
[IOcrDocumentProcessingService]
        ↓
[QueuedOcrProcessingService]  ← Channel<OcrQueueItem> محدود بـ 32 طلب
        ↓
[LocalPythonOcrProcessingService]
        ↓
[subprocess: python ocr_worker.py --request {json_file}]
        ↓
[OcrDocumentProcessingResult → Fields: List<OcrDocumentFieldCandidateDto>]
```

**الـ DTO الحالي لكل حقل:**
```csharp
record OcrDocumentFieldCandidateDto(
    string FieldKey,       // "guaranteeNumber", "amount", "expiryDate", ...
    string Value,          // القيمة المستخرجة
    int ConfidencePercent, // موجود لكن مصدره واحد
    int PageNumber,
    string? BoundingBox,   // موجود
    string? SourceLabel    // موجود: "native-text" أو "ocr-paddleocr"
);
```

### 1.2 خط الاستخراج الحالي (Sequential Waterfall)

```
PDF يُفتح بـ PyMuPDF
        ↓
[Tier 1] extract_native_text() — PyMuPDF get_text()
        ↓ إذا النص >= 20 حرف لكل صفحة → "text-native page"
        ↓ إذا < 20 حرف → "image-only page"
        ↓
[Tier 2 — للصفحات الممسوحة فقط]
   render_page() → PyMuPDF pixmap أو PDFium fallback
        ↓
   preprocess_image() → OpenCV: grayscale + denoise + Gaussian + Otsu threshold + morphology
        ↓
   detect_layout_regions() → LayoutParser بـ OpenCV contour detection
        ↓
   run_ocr() → PaddleOCR (lang="ar", cpu) على كل region منفصل
        ↓
[دمج النصوص] build_page_text() من كل المصادر
        ↓
[استخراج الحقول] regex + context patterns على النص المدمج
```

### 1.3 الحقول المستخرجة حالياً

| الحقل | الـ Key | آلية الاستخراج |
|-------|---------|----------------|
| رقم الضمان | `guaranteeNumber` | GUARANTEE_NUMBER_CONTEXT_REGEXES + is_likely_guarantee_number() |
| المبلغ | `amount` | AMOUNT_CONTEXT_REGEXES + extract_contextual_amount() |
| تاريخ الإصدار | `issueDate` | OFFICIAL_DATE_CONTEXT_REGEXES |
| تاريخ الانتهاء | `expiryDate` | EXPIRY_DATE_CONTEXT_REGEXES + TEXTUAL_EXPIRY_DATE_REGEX |
| المستفيد | `beneficiary` | extract_beneficiary_name() — fuzzy match للمستشفى |
| الجهة الضامنة | `principal` | extract_principal_name() |
| العملة | `currency` | CURRENCY_CODE_REGEX |
| البنك | `bankName` | looks_like_bank_name() + BANK_HINTS (9 بنوك) |
| المرجع البنكي | `bankReference` | BANK_REFERENCE_CONTEXT_REGEXES |

### 1.4 المشاكل الجوهرية في البنية الحالية

**المشكلة 1: Sequential لا Parallel**
النظام الحالي يشغّل OCR فقط إذا فشل النص المباشر.
لا يوجد مقارنة بين المصدرين — النص المباشر هو المصدر الوحيد للصفحات النصية.

**المشكلة 2: ConfidencePercent لا يعكس حالة التعدد**
الـ ConfidencePercent موجود في DTO لكنه مصدره مصدر واحد.
لا يوجد منطق "إذا اتفق مصدران → ثقة أعلى".

**المشكلة 3: Bank Detection موجود لكن لا يُخصص الاستخراج**
BANK_HINTS يكتشف البنك لكن الـ regex patterns هي نفسها لجميع البنوك.
كل بنك له قالب مختلف — بنك الراجحي يضع رقم الضمان في مكان مختلف عن الأهلي.

**المشكلة 4: لا Validation Graph**
لا يوجد تحقق متقاطع بين الحقول:
- هل تاريخ الانتهاء > تاريخ الإصدار؟
- هل العملة المستخرجة متوافقة مع المبلغ؟
- هل رقم الضمان ليس رقم سجل تجاري؟

**المشكلة 5: لا Feedback Loop**
تصحيحات المراجعين البشريين لا تُخزَّن كبيانات لتحسين الاستخراج.

---

## القسم الثاني: البنية المستهدفة (Ensemble Architecture)

### 2.1 المبدأ الأساسي

```
بدلاً من: إذا فشل A → جرّب B
المطلوب:  شغّل A و B (و C اختياري) → قارن → اتخذ قرار ذكي
```

### 2.2 خط الاستخراج الجديد

```
PDF وارد
        ↓
[Page Classifier] — لكل صفحة مستقلة
    ├── "text-native"  (>= 50 حرف من PyMuPDF)
    ├── "mixed"        (20-49 حرف)
    └── "image-only"   (< 20 حرف)
        ↓
[Parallel Extraction — يعمل بالتوازي]
    ├── Source A: PyMuPDF Native Text Extractor
    ├── Source B: PaddleOCR Pipeline (حتى للصفحات النصية)  ← الجديد
    └── Source C: LLM Vision (اختياري — فقط للحالات الغامضة)  ← مستقبلي
        ↓
[Bank Fingerprinting] — تحديد البنك مبكراً لتخصيص الاستخراج
        ↓
[Bank-Specific Field Extraction] — per-bank patterns
        ↓
[Field-Level Consensus Engine]
    لكل حقل: هل المصادر متفقة؟
        ↓
[Validation Graph] — تحقق متقاطع بين الحقول
        ↓
[Confidence Gating] — قرار: حفظ آلي / تحذير / مراجعة إلزامية
        ↓
[OcrDocumentProcessingResult] مع confidence مُحدَّثة لكل حقل
```

---

## القسم الثالث: التفاصيل التقنية لكل مكوّن

### 3.1 Page Classifier (تحسين الموجود)

**الحالي:** حد 20 حرف فقط  
**المطلوب:** تصنيف أدق

```python
def classify_page(page_text: str, page_image_path: str | None = None) -> PageType:
    char_count = len(page_text.strip())
    
    if char_count >= 50:
        # نص كافٍ — لكن تحقق: هل النص مقروء أم رموز غير مفهومة؟
        readable_ratio = count_readable_chars(page_text) / max(char_count, 1)
        if readable_ratio >= 0.7:
            return PageType.TEXT_NATIVE    # PyMuPDF كافٍ وموثوق
        else:
            return PageType.MIXED         # النص موجود لكن مشكوك فيه → شغّل OCR أيضاً
    
    elif char_count >= 20:
        return PageType.MIXED             # شغّل كليهما
    
    else:
        return PageType.IMAGE_ONLY        # OCR إلزامي

def count_readable_chars(text: str) -> int:
    # يعدّ الأحرف العربية والإنجليزية والأرقام — يتجاهل الرموز الغريبة
    return len(re.findall(r'[\u0600-\u06FF\u0750-\u077F\w]', text))
```

**القاعدة الجديدة:** كل صفحة MIXED أو IMAGE_ONLY تشغّل Source A و B معاً.  
صفحات TEXT_NATIVE: Source A أساسي + Source B كتحقق سريع (اختياري في المرحلة الأولى).

---

### 3.2 Bank Fingerprinting (تعميق الموجود)

**الحالي:** يكتشف البنك من النص فقط، يعيد اسماً كنصية.  
**المطلوب:** يعيد BankProfile كامل يُخصص الاستخراج.

```python
# هيكل البيانات الجديد
@dataclass
class BankExtractionProfile:
    canonical_name: str
    
    # أين يكون رقم الضمان في قالب هذا البنك؟
    guarantee_number_region: str  # "header", "body", "footer", "any"
    guarantee_number_patterns: list[re.Pattern]  # patterns خاصة بهذا البنك
    
    # هل يكتب البنك المبلغ بالحروف عادةً؟
    amount_in_words: bool  # بعض البنوك تكتب "خمسمائة ألف ريال"
    
    # نمط تاريخ الانتهاء المستخدم في هذا البنك
    date_format_hint: str  # "dd/mm/yyyy", "yyyy-mm-dd", "arabic-words"
    
    # نمط رقم الضمان الخاص بهذا البنك
    guarantee_number_format: str | None  # مثال: r"\d{2}[A-Z]{4}\d{8}" للراجحي

# profiles البنوك — تُبنى من بيانات حقيقية
BANK_PROFILES: dict[str, BankExtractionProfile] = {
    "Al Rajhi Bank": BankExtractionProfile(
        canonical_name="Al Rajhi Bank",
        guarantee_number_region="header",
        guarantee_number_patterns=[
            re.compile(r"\b\d{2}[A-Z]{4}\d{8,12}\b"),  # مثال: 23OGTE48803516
            re.compile(r"\bBG-\d{4}-\d{4,8}\b", re.IGNORECASE),
        ],
        amount_in_words=True,
        date_format_hint="dd/mm/yyyy",
        guarantee_number_format=r"\d{2}[A-Z]{4}\d{8,12}"
    ),
    "Saudi National Bank": BankExtractionProfile(
        canonical_name="Saudi National Bank",
        guarantee_number_region="body",
        guarantee_number_patterns=[
            re.compile(r"\bSNB[/-]?\d{6,14}\b", re.IGNORECASE),
            re.compile(r"\b[A-Z]{2,4}\d{8,14}\b"),
        ],
        amount_in_words=False,
        date_format_hint="yyyy-mm-dd",
        guarantee_number_format=None  # غير محدد — يُبنى من بيانات حقيقية
    ),
    # ... باقي البنوك تُضاف من بيانات Ground Truth
}

def get_bank_profile(detected_bank_name: str | None) -> BankExtractionProfile | None:
    if not detected_bank_name:
        return None
    return BANK_PROFILES.get(detected_bank_name)
```

**ملاحظة مهمة:** هذه الـ profiles تُبنى من فحص عينات حقيقية من كل بنك.  
ابدأ بـ 3 بنوك الأكثر تكراراً في مستنداتكم.

---

### 3.3 Parallel Extraction Engine

**التغيير الجوهري:** بدلاً من if/else — تشغيل متوازٍ مع دمج النتائج.

```python
@dataclass
class FieldExtraction:
    value: str | None
    confidence: float          # 0.0 → 1.0
    source: str                # "native-text", "paddleocr", "llm-vision"
    bounding_box: str | None
    page_number: int

@dataclass  
class MultiSourceFieldResult:
    field_key: str
    extractions: list[FieldExtraction]  # من كل مصدر
    consensus_value: str | None
    final_confidence: float
    needs_review: bool
    review_reason: str | None

def extract_all_sources(
    file_path: str,
    page_texts: list[tuple[int, str]],    # من PyMuPDF
    page_images: list[tuple[int, str]],   # مسارات الصور المُعالَجة
    bank_profile: BankExtractionProfile | None,
) -> dict[str, list[FieldExtraction]]:
    
    results: dict[str, list[FieldExtraction]] = defaultdict(list)
    
    # Source A: النص المباشر من PyMuPDF
    native_fields = extract_fields_from_native_text(page_texts, bank_profile)
    for field_key, extraction in native_fields.items():
        extraction.source = "native-text"
        results[field_key].append(extraction)
    
    # Source B: PaddleOCR — حتى للصفحات التي استُخرج منها نص مباشر
    # (للتحقق وليس كبديل فقط)
    ocr_fields = extract_fields_from_ocr(page_images, bank_profile)
    for field_key, extraction in ocr_fields.items():
        extraction.source = "paddleocr"
        results[field_key].append(extraction)
    
    # Source C: LLM Vision — مستقبلي، يُستدعى فقط إذا اختلف A وB
    # (Ollama + Qwen2-VL محلياً، أو يُعطل)
    
    return results
```

---

### 3.4 Consensus Engine (القلب الذكي)

```python
def compute_consensus(
    field_key: str,
    extractions: list[FieldExtraction],
    bank_profile: BankExtractionProfile | None,
) -> MultiSourceFieldResult:
    
    if not extractions:
        return MultiSourceFieldResult(
            field_key=field_key,
            extractions=[],
            consensus_value=None,
            final_confidence=0.0,
            needs_review=True,
            review_reason="no-extraction-result"
        )
    
    if len(extractions) == 1:
        # مصدر واحد فقط — ثقة محدودة
        e = extractions[0]
        return MultiSourceFieldResult(
            field_key=field_key,
            extractions=extractions,
            consensus_value=e.value,
            final_confidence=e.confidence * 0.8,  # خصم 20% لعدم التحقق
            needs_review=e.confidence < 0.85,
            review_reason="single-source-only" if e.confidence < 0.85 else None
        )
    
    # --- منطق التصويت ---
    
    # 1. تطبيع القيم قبل المقارنة
    normalized_groups: dict[str, list[FieldExtraction]] = defaultdict(list)
    for e in extractions:
        key = normalize_for_comparison(field_key, e.value)
        normalized_groups[key].append(e)
    
    # 2. ابحث عن أكبر مجموعة اتفاق
    majority_key = max(normalized_groups, key=lambda k: sum(e.confidence for e in normalized_groups[k]))
    majority_group = normalized_groups[majority_key]
    total_confidence = sum(e.confidence for e in extractions)
    agreement_confidence = sum(e.confidence for e in majority_group)
    agreement_ratio = agreement_confidence / max(total_confidence, 0.001)
    
    # 3. اختر القيمة الأفضل من المجموعة المتفقة
    best_extraction = max(majority_group, key=lambda e: e.confidence)
    
    # 4. احسب الـ confidence النهائية
    if agreement_ratio >= 0.80:
        # 80%+ من الثقة في نفس الإجابة → ثقة عالية
        final_confidence = min(0.98, agreement_confidence / len(majority_group) * 1.2)
        needs_review = False
        reason = None
        
    elif agreement_ratio >= 0.60:
        # اتفاق جزئي
        final_confidence = agreement_confidence / len(majority_group)
        needs_review = final_confidence < 0.75
        reason = "partial-agreement" if needs_review else None
        
    else:
        # خلاف حقيقي بين المصادر
        final_confidence = best_extraction.confidence * 0.5
        needs_review = True
        reason = f"sources-disagree: {[e.value for e in extractions]}"
    
    return MultiSourceFieldResult(
        field_key=field_key,
        extractions=extractions,
        consensus_value=best_extraction.value,
        final_confidence=final_confidence,
        needs_review=needs_review,
        review_reason=reason
    )


def normalize_for_comparison(field_key: str, value: str | None) -> str:
    """تطبيع القيم قبل المقارنة — حل مشكلة الاختلافات الشكلية"""
    if not value:
        return ""
    
    v = unicodedata.normalize("NFKC", value)
    v = v.translate(ARABIC_NUMERAL_TRANSLATION)  # ٠١٢ → 012
    
    if field_key == "amount":
        # أزل الفواصل، اجعلها رقم نظيف
        v = re.sub(r"[,،\s]", "", v)
        v = re.sub(r"\.00$", "", v)  # 500000.00 == 500000
    
    elif field_key in ("issueDate", "expiryDate"):
        # حوّل جميع صيغ التاريخ لـ yyyy-mm-dd
        v = normalize_date_value(v)
    
    elif field_key == "beneficiary":
        # أزل الاختلافات في الهمزات والتشكيل
        v = normalize_arabic_phrase(v)
    
    elif field_key == "guaranteeNumber":
        # أزل المسافات والشرطات — BG-2024-001 == BG2024001
        v = re.sub(r"[\s\-/]", "", v).upper()
    
    return v.strip().lower()
```

---

### 3.5 Validation Graph (التحقق المتقاطع)

```python
@dataclass
class ValidationIssue:
    severity: str      # "error", "warning"
    field_key: str
    message: str
    code: str

def validate_extracted_fields(
    fields: dict[str, MultiSourceFieldResult],
    bank_profile: BankExtractionProfile | None,
) -> list[ValidationIssue]:
    
    issues: list[ValidationIssue] = []
    
    # --- قواعد التواريخ ---
    issue_date = parse_date_safe(fields.get("issueDate"))
    expiry_date = parse_date_safe(fields.get("expiryDate"))
    
    if issue_date and expiry_date:
        if expiry_date <= issue_date:
            issues.append(ValidationIssue(
                severity="error",
                field_key="expiryDate",
                message="تاريخ الانتهاء يجب أن يكون بعد تاريخ الإصدار",
                code="date.expiry-before-issue"
            ))
        
        delta_days = (expiry_date - issue_date).days
        if delta_days > 365 * 10:  # أكثر من 10 سنوات — مشبوه
            issues.append(ValidationIssue(
                severity="warning",
                field_key="expiryDate",
                message=f"مدة الضمان {delta_days // 365} سنة — تحقق من التواريخ",
                code="date.unusually-long-duration"
            ))
        
        if delta_days < 30:  # أقل من شهر — مشبوه
            issues.append(ValidationIssue(
                severity="warning",
                field_key="expiryDate", 
                message="مدة الضمان أقل من 30 يوماً — تحقق",
                code="date.unusually-short-duration"
            ))
    
    # --- قواعد المبلغ ---
    amount_str = get_value(fields, "amount")
    if amount_str:
        try:
            amount = float(amount_str.replace(",", ""))
            if amount < 1000:
                issues.append(ValidationIssue(
                    severity="warning",
                    field_key="amount",
                    message=f"مبلغ {amount} منخفض جداً لضمان بنكي — تحقق",
                    code="amount.suspiciously-low"
                ))
            if amount > 500_000_000:  # أكثر من 500 مليون
                issues.append(ValidationIssue(
                    severity="warning",
                    field_key="amount",
                    message="مبلغ ضخم جداً — يتطلب مراجعة متأنية",
                    code="amount.unusually-high"
                ))
        except ValueError:
            issues.append(ValidationIssue(
                severity="error",
                field_key="amount",
                message="المبلغ المستخرج ليس رقماً صالحاً",
                code="amount.not-numeric"
            ))
    
    # --- تحقق رقم الضمان ليس رقم سجل تجاري ---
    guarantee_num = get_value(fields, "guaranteeNumber")
    if guarantee_num:
        # السجلات التجارية السعودية: 10 أرقام تبدأ بـ 1 أو 2
        if re.fullmatch(r"[12]\d{9}", guarantee_num):
            issues.append(ValidationIssue(
                severity="error",
                field_key="guaranteeNumber",
                message="رقم الضمان يشبه رقم سجل تجاري سعودي (10 أرقام)",
                code="guarantee-number.looks-like-cr"
            ))
    
    # --- تحقق العملة مع المبلغ ---
    currency = get_value(fields, "currency")
    if currency and amount_str:
        # إذا العملة USD والمبلغ يُذكر بعده "ريال" — خطأ
        # هذا يتطلب تحليل نصي أعمق — يُضاف لاحقاً
        pass
    
    # --- تحقق المستفيد ---
    beneficiary = get_value(fields, "beneficiary")
    if beneficiary:
        # يجب أن يحتوي على كلمة "مستشفى" أو "KFSH" أو ما يشابه
        kfsh_markers = ["مستشفى", "فيصل", "kfsh", "king faisal", "مركز الأبحاث"]
        if not any(m in beneficiary.lower() for m in kfsh_markers):
            issues.append(ValidationIssue(
                severity="warning",
                field_key="beneficiary",
                message="اسم المستفيد لا يتطابق مع المستشفى — تحقق",
                code="beneficiary.unexpected-name"
            ))
    
    return issues


def determine_gating_decision(
    fields: dict[str, MultiSourceFieldResult],
    validation_issues: list[ValidationIssue],
) -> tuple[str, str | None]:
    """
    يعيد: (decision, reason)
    decision: "auto-save" | "review-recommended" | "review-required"
    """
    
    # أي خطأ validation → مراجعة إلزامية
    if any(i.severity == "error" for i in validation_issues):
        reasons = [i.message for i in validation_issues if i.severity == "error"]
        return "review-required", f"أخطاء تحقق: {'; '.join(reasons)}"
    
    # الحقول الحرجة التي يجب أن تكون بثقة عالية
    CRITICAL_FIELDS = {
        "guaranteeNumber": 0.85,
        "amount":          0.85,
        "expiryDate":      0.80,
    }
    
    LOW_CONFIDENCE_FIELDS = []
    for field_key, min_confidence in CRITICAL_FIELDS.items():
        result = fields.get(field_key)
        if result is None or result.consensus_value is None:
            LOW_CONFIDENCE_FIELDS.append(f"{field_key} (مفقود)")
        elif result.final_confidence < min_confidence:
            LOW_CONFIDENCE_FIELDS.append(
                f"{field_key} ({result.final_confidence:.0%} < {min_confidence:.0%})"
            )
    
    if LOW_CONFIDENCE_FIELDS:
        return "review-required", f"ثقة منخفضة في: {', '.join(LOW_CONFIDENCE_FIELDS)}"
    
    # حقول تحتاج مراجعة (needs_review=True من الـ consensus)
    REVIEW_FIELDS = [
        field_key for field_key, result in fields.items()
        if result.needs_review
    ]
    
    if REVIEW_FIELDS:
        return "review-recommended", f"يُنصح بمراجعة: {', '.join(REVIEW_FIELDS)}"
    
    # تحذيرات validation
    if validation_issues:  # severity == "warning" فقط
        return "review-recommended", "; ".join(i.message for i in validation_issues)
    
    return "auto-save", None
```

---

### 3.6 تغييرات على C# Side

#### 3.6.1 توسيع OcrDocumentFieldCandidateDto

```csharp
// الحالي:
record OcrDocumentFieldCandidateDto(
    string FieldKey,
    string Value,
    int ConfidencePercent,
    int PageNumber,
    string? BoundingBox,
    string? SourceLabel);

// المطلوب — إضافة:
record OcrDocumentFieldCandidateDto(
    string FieldKey,
    string Value,
    int ConfidencePercent,
    int PageNumber,
    string? BoundingBox,
    string? SourceLabel,
    // جديد:
    bool NeedsReview,              // هل يحتاج هذا الحقل مراجعة بشرية؟
    string? ReviewReason,          // سبب طلب المراجعة
    IReadOnlyList<string> Sources, // ["native-text", "paddleocr"] — المصادر التي استُخرج منها
    bool SourcesAgreed);           // هل اتفقت المصادر؟
```

#### 3.6.2 توسيع OcrDocumentProcessingResult

```csharp
// إضافة:
record OcrDocumentProcessingResult(
    ...existing fields...,
    // جديد:
    string GatingDecision,          // "auto-save" | "review-recommended" | "review-required"
    string? GatingReason,           // سبب القرار
    IReadOnlyList<string> ValidationIssues  // أخطاء التحقق المتقاطع
);
```

#### 3.6.3 طبقة تخزين الـ Feedback (جديدة كلياً)

```csharp
// في BG.Domain:
public sealed class OcrFeedbackRecord
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public string FieldKey { get; init; } = string.Empty;
    public string ExtractedValue { get; init; } = string.Empty;
    public string CorrectedValue { get; init; } = string.Empty;  // ما أدخله المراجع
    public string? BankName { get; init; }
    public string? PipelineVersion { get; init; }
    public IReadOnlyList<string> Sources { get; init; } = [];
    public bool SourcesAgreed { get; init; }
    public int ConfidencePercent { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public string RecordedByUserId { get; init; } = string.Empty;
}
```

```csharp
// في BG.Application:
public interface IOcrFeedbackService
{
    Task RecordCorrectionAsync(
        Guid documentId,
        string fieldKey,
        string extractedValue,
        string correctedValue,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<OcrFeedbackSummaryDto>> GetFieldAccuracySummaryAsync(
        string? bankName = null,
        DateTimeOffset? from = null,
        CancellationToken cancellationToken = default);
}
```

---

### 3.7 واجهة المراجعة البشرية (المطلوبة)

#### ما يجب أن يُعرض للمراجع لكل حقل:

```
┌─────────────────────────────────────────────────────────┐
│  رقم الضمان                                              │
│  ┌─────────────────────────────────────────────────┐   │
│  │  23OGTE48803516                           [تعديل] │   │
│  └─────────────────────────────────────────────────┘   │
│                                                          │
│  الثقة: ████████░░ 80%    المصادر: PyMuPDF ✓  OCR ✓   │
│  المصادر اتفقت ✓                                        │
│  الصفحة 1 — الموقع: أعلى الصفحة يسار                  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  تاريخ الانتهاء                           ⚠️ تحقق     │
│  ┌─────────────────────────────────────────────────┐   │
│  │  2026-03-15                               [تعديل] │   │
│  └─────────────────────────────────────────────────┘   │
│                                                          │
│  الثقة: ████░░░░░░ 45%    المصادر: PyMuPDF ✗  OCR ✓   │
│  ⚠️ المصادر اختلفت: PyMuPDF: 2026-03-15 / OCR: 2025-03-15 │
│  الصفحة 1 — انقر لرؤية الموقع في المستند              │
└─────────────────────────────────────────────────────────┘
```

**المعلومات المطلوبة في الـ UI لكل حقل:**
1. القيمة المستخرجة + زر تعديل
2. شريط الثقة بالنسبة المئوية + اللون (أخضر/أصفر/أحمر)
3. المصادر التي استُخرج منها وهل اتفقت
4. إذا اختلفت: عرض قيمة كل مصدر
5. رقم الصفحة + الموقع التقريبي
6. سبب طلب المراجعة إن وُجد

---

## القسم الرابع: LLM كمصدر ثالث (مستقبلي)

### 4.1 لماذا هو اختياري وليس أساسياً؟

- يتطلب GPU أو نموذج محلي ثقيل (Qwen2-VL، Mistral-OCR)
- بطيء نسبياً (5-15 ثانية إضافية)
- يستحق التعقيد فقط عندما يختلف المصدران الآخران

### 4.2 كيف يُضاف لاحقاً

```python
# يُستدعى فقط إذا: اختلف native-text و paddleocr على حقل واحد أو أكثر
def extract_from_llm_vision(
    page_image_path: str,
    fields_in_conflict: list[str],
    bank_name: str | None,
) -> dict[str, FieldExtraction] | None:
    
    # التحقق: هل LLM مفعّل ومتاح؟
    if not LLM_ENABLED or llm_client is None:
        return None
    
    prompt = build_extraction_prompt(fields_in_conflict, bank_name)
    
    try:
        response = llm_client.chat(
            model="qwen2-vl:7b",  # Ollama محلي
            messages=[{
                "role": "user",
                "content": [
                    {"type": "image", "path": page_image_path},
                    {"type": "text",  "text": prompt}
                ]
            }]
        )
        return parse_llm_response(response, fields_in_conflict)
    except Exception:
        return None  # فشل LLM لا يوقف العملية
```

**متطلبات التفعيل:**
- تثبيت Ollama على خادم الإنتاج
- تحميل نموذج: `ollama pull qwen2-vl:7b` (~5 GB)
- إضافة `LLM_ENABLED=true` في إعدادات البيئة

---

## القسم الخامس: قاعدة بيانات Ground Truth

### 5.1 لماذا هي إلزامية؟

بدون بيانات Ground Truth:
- لا يمكن قياس التحسن الفعلي
- لا يمكن معايرة حدود الـ confidence
- لا يمكن التحقق من دقة الـ bank profiles

### 5.2 ما تحتاجه

```
لكل مستند في العينة:
- الملف PDF الأصلي
- القيم الصحيحة لكل حقل (يدوياً من موظف معتمد)
- اسم البنك
- نوع المستند (ضمان إصدار / تمديد / ...)

الهدف: 100-150 مستند
التوزيع: 10-15 مستند لكل بنك رئيسي على الأقل
```

### 5.3 ملف Ground Truth (CSV)

```csv
document_id,bank_name,guarantee_number,amount,issue_date,expiry_date,currency,beneficiary,principal,notes
DOC001,Al Rajhi Bank,23OGTE48803516,500000,2024-01-15,2025-01-15,SAR,مستشفى الملك فيصل,شركة ABC,
DOC002,Saudi National Bank,SNB-2024-00123,1200000,2024-02-01,2026-02-01,SAR,مستشفى الملك فيصل,مؤسسة XYZ,مبلغ بالحروف
...
```

### 5.4 سكريبت القياس

```python
# scripts/evaluate_extraction.py
def evaluate(ground_truth_csv: str, documents_folder: str):
    results = []
    
    for row in load_csv(ground_truth_csv):
        extracted = run_extraction_pipeline(
            file_path=f"{documents_folder}/{row.document_id}.pdf",
            bank_profile_key=row.bank_name
        )
        
        for field_key in FIELD_KEYS:
            gt_value = normalize_for_comparison(field_key, row[field_key])
            ex_value = normalize_for_comparison(field_key, extracted.get(field_key))
            
            results.append({
                "bank": row.bank_name,
                "field": field_key,
                "match": gt_value == ex_value,
                "gt": gt_value,
                "extracted": ex_value,
                "confidence": extracted.get_confidence(field_key),
            })
    
    # طباعة التقرير
    print_accuracy_report(results)

# مثال على الإخراج:
# Field          | Total | Match | Accuracy | Avg Conf
# guaranteeNumber|  100  |  87   |   87%    |   0.82
# amount         |  100  |  93   |   93%    |   0.89
# expiryDate     |  100  |  78   |   78%    |   0.74
# issueDate      |  100  |  81   |   81%    |   0.77
```

---

## القسم السادس: Dashboard وجودة التشغيل

### 6.1 ما يجب قياسه أسبوعياً

```
لكل بنك ولكل حقل:
┌──────────────────┬────────────┬────────────┬──────────────┬─────────────┐
│ البنك            │ الحقل      │ دقة الأسبوع│ اتفاق المصادر│ نسبة المراجعة│
├──────────────────┼────────────┼────────────┼──────────────┼─────────────┤
│ Al Rajhi Bank    │ amount     │    95%     │    88%       │    12%      │
│ Al Rajhi Bank    │ expiryDate │    82%     │    71%       │    28%      │ ← تحتاج تحسين
│ Saudi National   │ guarantee  │    90%     │    85%       │    15%      │
└──────────────────┴────────────┴────────────┴──────────────┴─────────────┘

المقياس الرئيسي: نسبة المستندات التي وصلت لـ "حفظ آلي" دون تصحيح بشري
الهدف: >= 70% في نهاية الشهر الأول، >= 85% بعد 3 أشهر
```

### 6.2 ربط قاعدة بيانات Feedback بالـ Dashboard

كل تصحيح من المراجع البشري يُسجَّل في OcrFeedbackRecord.  
تقرير أسبوعي يُحلل:
- أكثر الحقول تصحيحاً per bank → تُحسَّن أولاً
- القيم المكررة التي تُصحَّح دائماً → regex جديد يُضاف للـ bank profile
- الحالات التي اختلفت فيها المصادر وكان أحدها صحيحاً → تحسين معادلة الـ consensus

---

## القسم السابع: خطة التنفيذ

### المرحلة صفر — التحضير (3-5 أيام، قبل أي كود)

- [ ] فحص 20 مستند حقيقي: كم % منها text-native؟ كم % صور؟
- [ ] تحديد البنوك الثلاثة الأكثر تكراراً
- [ ] جمع 15+ مستند من كل بنك من هذه الثلاثة
- [ ] إدخال القيم الصحيحة يدوياً (Ground Truth أولي)
- [ ] تشغيل الكود الحالي على هذه العينة → baseline metrics

**المخرج:** رقم baseline واضح: "دقة الاستخراج الحالية X% لكل حقل لكل بنك"

---

### المرحلة الأولى — Page Classifier + Bank Profiles (أسبوع 1)

**ملفات Python تُعدَّل:**
- `ocr_worker.py` — إضافة `classify_page()` المُحسَّنة
- `ocr_worker.py` — إضافة `BankExtractionProfile` dataclass
- `ocr_worker.py` — بناء profiles للبنوك الثلاثة الرئيسية من Ground Truth

**اختبار:** تشغيل على Ground Truth → قياس تحسن الدقة مقارنة بـ baseline

---

### المرحلة الثانية — Parallel Extraction + Consensus (أسبوع 2)

**ملفات Python تُعدَّل:**
- `ocr_worker.py` — إضافة `extract_all_sources()` (تشغيل النص المباشر والـ OCR معاً)
- `ocr_worker.py` — إضافة `compute_consensus()` كاملاً
- `ocr_worker.py` — إضافة `normalize_for_comparison()` لكل حقل

**ملفات C# تُعدَّل:**
- `OcrDocumentFieldCandidateDto.cs` — إضافة الحقول الجديدة

**اختبار:** نفس Ground Truth → هل الـ consensus أدق من مصدر واحد؟

---

### المرحلة الثالثة — Validation Graph + Gating (أسبوع 3)

**ملفات Python تُعدَّل:**
- `ocr_worker.py` — إضافة `validate_extracted_fields()`
- `ocr_worker.py` — إضافة `determine_gating_decision()`

**ملفات C# تُعدَّل:**
- `OcrDocumentProcessingResult.cs` — إضافة `GatingDecision` و `ValidationIssues`

**اختبار:** هل الـ gating يمسك الحالات التي يُصحِّحها المراجعون؟

---

### المرحلة الرابعة — UI المراجعة + Feedback Loop (أسبوع 4)

**ملفات C# جديدة:**
- `OcrFeedbackRecord.cs` في BG.Domain
- `IOcrFeedbackService.cs` و `OcrFeedbackService.cs` في BG.Application
- Migration لجدول `OcrFeedbackRecords`

**ملفات Razor Pages تُعدَّل:**
- شاشة مراجعة الـ OCR — إضافة confidence display وعرض تعارض المصادر
- استدعاء `RecordCorrectionAsync()` عند تعديل المراجع

**اختبار:** مراجع حقيقي يعمل على مستندات فعلية → قياس وقت المراجعة

---

### المرحلة الخامسة — LLM Vision (اختياري، بعد استقرار 1-4)

- تقييم جدوى: هل تختلف المصادران بنسبة > 20%؟
- إذا نعم: إعداد Ollama + Qwen2-VL على الخادم
- تشغيل تجريبي على الحالات المتعارضة فقط
- قياس: هل LLM يحل الخلافات بدقة أعلى؟

---

## القسم الثامن: ملخص الفروق بين الحالي والمستهدف

| الجانب | الوضع الحالي | الهدف |
|--------|-------------|-------|
| مصادر الاستخراج | 1 مصدر (native أو OCR) | 2+ مصادر بالتوازي |
| آلية الاختيار | Waterfall fallback | Consensus voting |
| Bank profiles | كشف اسم البنك فقط | patterns وregions خاصة بكل بنك |
| Confidence | مصدر واحد | مُعزَّزة من اتفاق المصادر |
| Validation | تحقق فردي لكل حقل | رسم تحقق متقاطع بين الحقول |
| Gating | غير موجود | 3 مستويات: آلي / تحذير / إلزامي |
| Feedback | لا يُخزَّن | DB table + تحليل أسبوعي |
| قياس الدقة | غير موجود | Ground Truth + baseline + metrics |
| LLM | غير مدعوم | مستقبلي — للحالات المتعارضة فقط |

---

## القسم التاسع: القيود والمخاطر

### ما هو خارج النطاق (لا يتغير)
- لا خدمات سحابية — العمل داخل الشبكة الداخلية فقط
- Python يبقى في BG.Integrations فقط
- لا تغيير على بنية ASP.NET Core 8 أو Razor Pages
- العقد `IOcrDocumentProcessingService` لا يتغير من خارج BG.Integrations

### مخاطر التنفيذ
1. **الأداء:** تشغيل OCR حتى على الصفحات النصية يضاعف الوقت للصفحات المختلطة.  
   الحل: ابدأ بـ OCR كتحقق فقط للحقول الحرجة (amount, guaranteeNumber) لا لكل الصفحة.

2. **Ground Truth الناقص:** إذا كانت العينة صغيرة، ستكون الـ bank profiles غير دقيقة.  
   الحل: ابدأ بـ 3 بنوك فقط، وسّع تدريجياً.

3. **Arabic normalization في الـ consensus:** الأخطاء الشكلية (ي vs ى) ستُظهر اتفاقاً زائفاً أو خلافاً وهمياً.  
   الحل: `normalize_for_comparison()` يجب أن يُختبر على عينات حقيقية قبل الإنتاج.

---

*الملف مرجع حي — يُحدَّث بعد كل مرحلة بالنتائج الفعلية والدروس المستفادة.*
