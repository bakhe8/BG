import argparse
import contextlib
import json
import os
import re
import tempfile
import sys
import traceback
import unicodedata
import warnings
from pathlib import Path

os.environ.setdefault("PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK", "True")
os.environ.setdefault("PYTHONIOENCODING", "utf-8")
os.environ.setdefault("PYTHONUTF8", "1")
os.environ.setdefault("GLOG_minloglevel", "3")
os.environ.setdefault("FLAGS_logtostderr", "0")
os.environ.setdefault("HF_HUB_DISABLE_PROGRESS_BARS", "1")

warnings.filterwarnings("ignore", category=UserWarning)
warnings.filterwarnings("ignore", message=r"urllib3 .* doesn't match a supported version!")
warnings.filterwarnings("ignore", message=r"No ccache found\..*")
warnings.filterwarnings("ignore", category=DeprecationWarning)

try:
    from requests import RequestsDependencyWarning
except Exception:  # pragma: no cover
    RequestsDependencyWarning = None

if RequestsDependencyWarning is not None:
    warnings.filterwarnings("ignore", category=RequestsDependencyWarning)

try:
    import fitz  # PyMuPDF
except Exception:  # pragma: no cover - runtime-only dependency failure
    fitz = None

try:
    import cv2  # type: ignore
except Exception:  # pragma: no cover
    cv2 = None

try:
    import layoutparser as lp  # type: ignore
except Exception:  # pragma: no cover
    lp = None

try:
    from paddleocr import PaddleOCR  # type: ignore
except Exception:  # pragma: no cover
    PaddleOCR = None

try:
    import pypdfium2 as pdfium  # type: ignore
except Exception:  # pragma: no cover
    pdfium = None


DATE_FRAGMENT = r"(?:20\d{2}\s*[-/]\s*(?:0?[1-9]|1[0-2])\s*[-/]\s*(?:0?[1-9]|[12]\d|3[01])|(?:0?[1-9]|[12]\d|3[01])\s*[-/]\s*(?:0?[1-9]|1[0-2])\s*[-/]\s*20\d{2})"

GUARANTEE_NUMBER_REGEX = re.compile(r"BG-\d{4}-\d{3,6}", re.IGNORECASE)
GENERIC_GUARANTEE_CODE_REGEX = re.compile(r"\b[A-Z]{1,5}\d{6,16}\b")
COMPLEX_GUARANTEE_CODE_REGEX = re.compile(r"\b[A-Z]{1,6}(?:/[A-Z]{1,6})[-/]?\d{3,12}\b", re.IGNORECASE)
GUARANTEE_NUMBER_CONTEXT_REGEXES = [
    re.compile(r"(?:خطاب\s*ضمان\s*رقم|رقم\s*خطاب\s*الضمان|الضمان\s*رقم|ضمان\s*رقم)\s*[:\-]?\s*([A-Z0-9/-]{6,24})", re.IGNORECASE),
    re.compile(r"(?:guarantee\s*(?:number|no\.?|#))\s*[:\-]?\s*([A-Z0-9/-]{6,24})", re.IGNORECASE),
]
BANK_REFERENCE_REGEXES = [
    re.compile(r"\b(?:EXT|RED|REL|STAT)-[A-Z0-9-]{3,}\b", re.IGNORECASE),
    re.compile(r"\b[A-Z]{2,6}-OCR-W1\b", re.IGNORECASE),
    re.compile(r"\bCRT\d{6,12}\b", re.IGNORECASE),
]
BANK_REFERENCE_CONTEXT_REGEXES = [
    re.compile(r"(?:رقم\s*المرجع(?:\s*المتسلسل)?|المرجع(?:\s*المتسلسل)?|bank\s*reference|reference\s*(?:number|no\.?))\s*[:\-]?\s*([A-Z0-9/-]{5,30})", re.IGNORECASE),
]
BENEFICIARY_CONTEXT_REGEXES = [
    re.compile(r"(?:اسم\s*المستفيد|المستفيد|beneficiary)\s*[:\-]?\s*([^\n\r]{4,140})", re.IGNORECASE),
    re.compile(r"(?:السادة|المحترمين|to)\s*[:\-]?\s*(مستشفى[^\n\r]{6,180}|king faisal[^\n\r]{6,180})", re.IGNORECASE),
]
PRINCIPAL_CONTEXT_REGEXES = [
    re.compile(r"(?:اسم\s*(?:العميل|المقاول|المتقدم)|العميل|المقاول|المتقدم|principal|applicant|customer|contractor)\s*[:\-]?\s*([^\n\r]{4,140})", re.IGNORECASE),
    re.compile(r"(?:عملاءنا\s*السادة|عميلنا\s*السادة|عملائنا\s*السادة)\s*[:\-]?\s*([^\n\r]{6,180})", re.IGNORECASE),
    re.compile(r"(?:حيث\s+أنكم\s+منحتم\s+عملاءنا\s+السادة)\s*[:\-]?\s*([^\n\r]{6,220})", re.IGNORECASE),
]
DATE_REGEX = re.compile(rf"(?<!\d){DATE_FRAGMENT}(?!\d)")
OFFICIAL_DATE_CONTEXT_REGEXES = [
    re.compile(rf"(?:التاريخ|الموافق|date)\s*[:\-]?\s*({DATE_FRAGMENT})", re.IGNORECASE),
]
EXPIRY_DATE_CONTEXT_REGEXES = [
    re.compile(rf"(?:حتي\s*(?:نهاي[هة]\s*اليوم)?|ينتهي|انتهاء|ليصبح\s*ساري(?:\s*المفعول)?\s*حتي|valid\s*until|expiry(?:\s*date)?)\D{{0,50}}({DATE_FRAGMENT})", re.IGNORECASE),
]
AMOUNT_REGEX = re.compile(r"\b\d{2,3}(?:,\d{3})+(?:\.\d{2})?\b|\b\d{5,}(?:\.\d{2})?\b")
AMOUNT_CONTEXT_REGEXES = [
    re.compile(r"(?:القيمة|قيمته|قيمة\s*الضمان|قيمة)\s*[:#\-]?\s*(?:SAR\s*)?.{0,80}?([0-9][0-9,]{3,}(?:\.\d{2})?)", re.IGNORECASE),
    re.compile(r"(?:مبلغ(?:ا)?\s*وقدره|وقدره|بمبلغ|مبلغ)\s*[:#\-]?\s*(?:SAR\s*)?.{0,80}?([0-9][0-9,]{3,}(?:\.\d{2})?)", re.IGNORECASE),
    re.compile(r"(?:amount(?:\s+of)?|value(?:\s+of)?|sum\s+of)\s*[:#\-]?\s*(?:SAR|USD|EUR|GBP)?.{0,80}?([0-9][0-9,]{3,}(?:\.\d{2})?)", re.IGNORECASE),
    re.compile(r"#\s*([0-9][0-9,]{3,}(?:\.\d{2})?)\s*#", re.IGNORECASE),
]
CURRENCY_CODE_REGEX = re.compile(r"\b(?:SAR|USD|EUR|GBP)\b", re.IGNORECASE)

BANK_HINTS = [
    ("Saudi National Bank", ["saudi national bank", "snb", "البنك الأهلي السعودي", "الأهلي", "الاهلي"]),
    ("Riyad Bank", ["riyad bank", "بنك الرياض"]),
    ("SABB", ["sabb", "saudi awwal bank", "saudi british bank", "الأول", "ساب", "البنك السعودي البريطاني"]),
    ("Arab National Bank", ["arab national bank", "anb", "بنك العربي الوطني", "العربي الوطني", "البنك العربي الوطني"]),
    ("Banque Saudi Fransi", ["banque saudi fransi", "bsf", "saudi fransi", "البنك السعودي الفرنسي", "السعودي الفرنسي", "الفرنسي"]),
    ("Saudi Investment Bank", ["saudi investment bank", "saib", "البنك السعودي للاستثمار", "السعودي للاستثمار", "الاستثمار"]),
    ("Al Rajhi Bank", ["al rajhi bank", "alrajhi", "مصرف الراجحي", "الراجحي"]),
    ("Alinma Bank", ["alinma bank", "alinma", "بنك الإنماء", "مصرف الإنماء", "الإنماء", "الانماء"]),
    ("BNP Paribas", ["bnp paribas", "bnp", "paribas", "بي ان بي باريبا"]),
]

TEXTUAL_EXPIRY_DATE_REGEX = re.compile(
    r"(?:حتي(?:\s+نهاي[هة])?\s+اليوم|ينتهي|انتهاء|ليصبح\s+ساري(?:\s+المفعول)?\s+حتي|حتي)\s*:?\s*"
    r"([ا-ي\s]+?)\s+من\s+شهر\s+([ا-ي\s]+?)\s+(?:ل\S*سن[هة]|ل\s*سن[هة]|لسن[هة])\s+([ا-ي\s]+?)"
    r"(?:\s+للمي?ا?لاد|(?=\s+[.،,]|$)|(?=\s+وتظل)|(?=\s+وتبقى))",
    re.IGNORECASE,
)

ARABIC_DAY_VALUE_MAP = {
    "الاول": 1,
    "الاولى": 1,
    "الثاني": 2,
    "الثانيه": 2,
    "الثالث": 3,
    "الرابع": 4,
    "الخامس": 5,
    "السادس": 6,
    "السابع": 7,
    "الثامن": 8,
    "التاسع": 9,
    "العاشر": 10,
    "الحادي عشر": 11,
    "الثاني عشر": 12,
    "الثالث عشر": 13,
    "الرابع عشر": 14,
    "الخامس عشر": 15,
    "السادس عشر": 16,
    "السابع عشر": 17,
    "الثامن عشر": 18,
    "التاسع عشر": 19,
    "العشرون": 20,
    "الحادي والعشرون": 21,
    "الثاني والعشرون": 22,
    "الثالث والعشرون": 23,
    "الرابع والعشرون": 24,
    "الخامس والعشرون": 25,
    "السادس والعشرون": 26,
    "السابع والعشرون": 27,
    "الثامن والعشرون": 28,
    "التاسع والعشرون": 29,
    "الثلاثون": 30,
    "الحادي والثلاثون": 31,
    "الحادي والثالثون": 31,
}

ARABIC_MONTH_VALUE_MAP = {
    "يناير": 1,
    "فبراير": 2,
    "مارس": 3,
    "ما رس": 3,
    "ابريل": 4,
    "ابريل": 4,
    "ابرايل": 4,
    "مايو": 5,
    "يونيو": 6,
    "يوليو": 7,
    "اغسطس": 8,
    "سبتمبر": 9,
    "اكتوبر": 10,
    "نوفمبر": 11,
    "ديسمبر": 12,
}

ARABIC_NUMBER_VALUE_MAP = {
    "صفر": 0,
    "واحد": 1,
    "واحده": 1,
    "احد": 1,
    "اثنان": 2,
    "اثنين": 2,
    "اثنتان": 2,
    "اثنتين": 2,
    "اثنا": 2,
    "ثلاثه": 3,
    "ثلاث": 3,
    "اربعه": 4,
    "اربع": 4,
    "خمسه": 5,
    "خمس": 5,
    "سته": 6,
    "ست": 6,
    "سبعه": 7,
    "سبع": 7,
    "ثمانيه": 8,
    "ثمان": 8,
    "تسعه": 9,
    "تسع": 9,
    "عشره": 10,
    "عشر": 10,
    "عشرون": 20,
    "عشرين": 20,
    "ثلاثون": 30,
    "ثلاثين": 30,
    "اربعون": 40,
    "اربعين": 40,
    "خمسون": 50,
    "خمسين": 50,
    "ستون": 60,
    "ستين": 60,
    "سبعون": 70,
    "سبعين": 70,
    "ثمانون": 80,
    "ثمانين": 80,
    "تسعون": 90,
    "تسعين": 90,
}

PADDLE_OCR_INSTANCE = None


def canonical_result(succeeded: bool, fields: list[dict], warnings: list[str], error_code=None, error_message=None, pipeline_version="wave2-pdf-first"):
    return {
        "succeeded": succeeded,
        "processorName": "bg-python-ocr",
        "pipelineVersion": pipeline_version,
        "fields": fields,
        "warnings": warnings,
        "errorCode": error_code,
        "errorMessage": error_message,
    }


def read_request(request_path: Path) -> dict:
    return json.loads(request_path.read_text(encoding="utf-8-sig"))


def open_document(file_path: str):
    if fitz is None:
        raise RuntimeError("PyMuPDF is not installed.")

    return fitz.open(file_path)


def resolve_selected_page_numbers(document, request_payload: dict) -> list[int]:
    selected_pages = request_payload.get("selectedPages")
    if not selected_pages:
        return [index + 1 for index in range(len(document))]

    normalized_pages = []
    for value in selected_pages:
        try:
            page_number = int(value)
        except (TypeError, ValueError):
            continue

        if 1 <= page_number <= len(document) and page_number not in normalized_pages:
            normalized_pages.append(page_number)

    return normalized_pages or [index + 1 for index in range(len(document))]


def extract_native_text(document, page_numbers: list[int]) -> tuple[list[tuple[int, str]], int]:
    page_texts: list[str] = []
    usable_page_count = 0

    outputs: list[tuple[int, str]] = []
    for page_number in page_numbers:
        page = document[page_number - 1]
        text = (page.get_text("text") or "").strip()
        outputs.append((page_number, text))
        if len(text) >= 20:
            usable_page_count += 1

    return outputs, usable_page_count


def normalize_native_text(text: str) -> str:
    normalized = unicodedata.normalize("NFKC", text or "")
    normalized = normalized.translate(ARABIC_NUMERAL_TRANSLATION)
    normalized = normalized.replace("،", ",").replace("٫", ".")
    return re.sub(r"\s+", " ", normalized).strip()


def normalize_arabic_phrase(text: str) -> str:
    normalized = unicodedata.normalize("NFKC", text or "").lower()
    normalized = normalized.translate(ARABIC_NUMERAL_TRANSLATION)
    normalized = (
        normalized.replace("أ", "ا")
        .replace("إ", "ا")
        .replace("آ", "ا")
        .replace("ى", "ي")
        .replace("ؤ", "و")
        .replace("ئ", "ي")
        .replace("ـ", " ")
        .replace("ﻻ", "لا")
        .replace("ﻷ", "لا")
        .replace("ﻹ", "لا")
        .replace("ﻵ", "لا")
        .replace("ﻼ", "لا")
    )
    normalized = re.sub(r"[^\w\s/-]", " ", normalized)
    normalized = re.sub(r"\s+", " ", normalized).strip()
    return normalized


def lookup_arabic_phrase_value(mapping: dict[str, int], text: str) -> int | None:
    normalized = normalize_arabic_phrase(text).replace("ة", "ه")
    if not normalized:
        return None

    direct = mapping.get(normalized)
    if direct is not None:
        return direct

    compact = normalized.replace(" ", "")
    for key, value in mapping.items():
        if key.replace(" ", "") == compact:
            return value

    return None


def render_page_with_pymupdf(page, page_number: int) -> str:
    pix = page.get_pixmap(matrix=fitz.Matrix(3, 3), alpha=False)
    page_path = Path(tempfile.gettempdir()) / f"bg-ocr-page-{os.getpid()}-{page_number}.png"
    pix.save(page_path.as_posix())
    return page_path.as_posix()


def render_page_with_pdfium(file_path: str, page_number: int) -> str:
    if pdfium is None:
        raise RuntimeError("PDFium is not installed.")
    if cv2 is None:
        raise RuntimeError("OpenCV is required for PDFium raster fallback.")

    document = pdfium.PdfDocument(file_path)
    page = None
    bitmap = None
    try:
        page = document.get_page(page_number - 1)
        bitmap = page.render(scale=300 / 72, prefer_bgrx=True)
        image = bitmap.to_numpy()
        page_path = Path(tempfile.gettempdir()) / f"bg-ocr-page-{os.getpid()}-{page_number}-pdfium.png"
        cv2.imwrite(page_path.as_posix(), image)
        return page_path.as_posix()
    finally:
        if bitmap is not None:
            bitmap.close()
        if page is not None:
            page.close()
        document.close()


def render_page(file_path: str, page, page_number: int) -> tuple[str | None, list[str]]:
    try:
        return render_page_with_pymupdf(page, page_number), []
    except Exception:
        warnings = ["pdfium-raster-fallback"]

    try:
        return render_page_with_pdfium(file_path, page_number), warnings
    except Exception:
        warnings.append("pdfium-raster-failed")
        return None, warnings


def preprocess_image(image_path: str) -> tuple[str, list[str]]:
    warnings: list[str] = []
    if cv2 is None:
        warnings.append("opencv-missing")
        return image_path, warnings

    image = cv2.imread(image_path)
    if image is None:
        warnings.append("opencv-read-failed")
        return image_path, warnings

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    denoised = cv2.fastNlMeansDenoising(gray, None, 12, 7, 21)
    normalized = cv2.GaussianBlur(denoised, (3, 3), 0)
    _, thresholded = cv2.threshold(normalized, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (2, 2))
    cleaned = cv2.morphologyEx(thresholded, cv2.MORPH_OPEN, kernel)

    output_path = Path(tempfile.gettempdir()) / f"bg-ocr-pre-{Path(image_path).name}"
    cv2.imwrite(output_path.as_posix(), cleaned)
    return output_path.as_posix(), warnings


def detect_layout_regions(image_path: str) -> tuple[list[tuple[int, int, int, int]], list[str]]:
    warnings: list[str] = []

    if lp is None:
        warnings.append("layoutparser-missing")
        return [], warnings

    try:
        image = cv2.imread(image_path, cv2.IMREAD_GRAYSCALE) if cv2 is not None else None
        if image is None:
            warnings.append("layoutparser-image-unavailable")
            return [], warnings

        _, binary = cv2.threshold(image, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (40, 5))
        dilated = cv2.dilate(binary, kernel, iterations=2)
        contours, _ = cv2.findContours(dilated, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        height, width = image.shape[:2]

        blocks = []
        for contour in contours:
            x, y, w, h = cv2.boundingRect(contour)
            if w < 80 or h < 20:
                continue
            if w * h < 4000:
                continue

            rectangle = lp.Rectangle(x_1=x, y_1=y, x_2=x + w, y_2=y + h)
            blocks.append(lp.TextBlock(rectangle, type="text", score=1.0))

        if not blocks:
            warnings.append("layoutparser-fallback-full-page")
            return [(0, 0, width, height)], warnings

        layout = lp.Layout(blocks)
        ordered = sorted(
            layout,
            key=lambda block: (
                round(block.coordinates[1] / 20),
                block.coordinates[0],
            ),
        )
        regions = [
            (
                max(0, int(block.coordinates[0]) - 8),
                max(0, int(block.coordinates[1]) - 8),
                min(width, int(block.coordinates[2]) + 8),
                min(height, int(block.coordinates[3]) + 8),
            )
            for block in ordered
        ]
        return regions, warnings
    except Exception:
        warnings.append("layoutparser-detection-failed")
        return [], warnings


@contextlib.contextmanager
def suppress_process_output():
    stdout_fd = os.dup(1)
    stderr_fd = os.dup(2)
    devnull_fd = os.open(os.devnull, os.O_WRONLY)
    try:
        os.dup2(devnull_fd, 1)
        os.dup2(devnull_fd, 2)
        yield
    finally:
        os.dup2(stdout_fd, 1)
        os.dup2(stderr_fd, 2)
        os.close(stdout_fd)
        os.close(stderr_fd)
        os.close(devnull_fd)


def get_paddle_ocr():
    global PADDLE_OCR_INSTANCE

    if PaddleOCR is None:
        return None

    if PADDLE_OCR_INSTANCE is None:
        try:
            with suppress_process_output():
                PADDLE_OCR_INSTANCE = PaddleOCR(
                    lang="ar",
                    device="cpu",
                    enable_hpi=False,
                    enable_mkldnn=False,
                    use_doc_orientation_classify=False,
                    use_doc_unwarping=False,
                    use_textline_orientation=True,
                )
        except Exception:
            PADDLE_OCR_INSTANCE = False

    return None if PADDLE_OCR_INSTANCE is False else PADDLE_OCR_INSTANCE


def run_ocr(image_path: str, regions: list[tuple[int, int, int, int]] | None = None) -> tuple[list[tuple[str, str]], list[str]]:
    warnings: list[str] = []
    ocr = get_paddle_ocr()
    if ocr is None:
        warnings.append("paddleocr-missing-or-init-failed")
        return [], warnings

    try:
        image = cv2.imread(image_path) if cv2 is not None else None
        if image is None:
            warnings.append("paddleocr-image-unavailable")
            return [], warnings

        active_regions = regions or [(0, 0, image.shape[1], image.shape[0])]
        outputs: list[tuple[str, str]] = []

        for index, (x1, y1, x2, y2) in enumerate(active_regions):
            crop = image[y1:y2, x1:x2]
            if crop.size == 0:
                continue

            crop_path = Path(tempfile.gettempdir()) / f"bg-ocr-crop-{os.getpid()}-{index + 1}.png"
            cv2.imwrite(crop_path.as_posix(), crop)
            try:
                with suppress_process_output():
                    result = ocr.predict(crop_path.as_posix())
                if not result:
                    continue

                rec_texts = [text for text in result[0].get("rec_texts", []) if text]
                if rec_texts:
                    outputs.append((" ".join(str(text) for text in rec_texts), f"{x1},{y1},{x2},{y2}"))
            finally:
                if crop_path.exists():
                    crop_path.unlink()

        return outputs, warnings
    except Exception:
        warnings.append("paddleocr-run-failed")
        return [], warnings


def extract_header_regions(image_path: str) -> tuple[list[tuple[str, str]], list[str]]:
    warnings: list[str] = []
    if cv2 is None:
        warnings.append("opencv-missing")
        return [], warnings

    image = cv2.imread(image_path)
    if image is None:
        warnings.append("header-image-unavailable")
        return [], warnings

    height, width = image.shape[:2]
    header_height = max(300, int(height * 0.32))
    crop = image[0:header_height, 0:width]
    crop_path = Path(tempfile.gettempdir()) / f"bg-ocr-header-{os.getpid()}-{Path(image_path).name}"

    try:
        cv2.imwrite(crop_path.as_posix(), crop)
        results, ocr_warnings = run_ocr(crop_path.as_posix(), None)
        warnings.extend(ocr_warnings)
        return results, warnings
    finally:
        if crop_path.exists():
            crop_path.unlink()


def find_first(regexes: list[re.Pattern], text: str) -> str | None:
    for regex in regexes:
        match = regex.search(text)
        if match:
            return match.group(0)
    return None


def looks_like_bank_name(text: str, request_payload: dict) -> str | None:
    canonical_bank_name = (request_payload.get("canonicalBankName") or "").strip()
    if canonical_bank_name and canonical_bank_name.lower() in text.lower():
        return canonical_bank_name

    normalized = unicodedata.normalize("NFKC", text or "")
    normalized = normalized.translate(ARABIC_NUMERAL_TRANSLATION).replace("،", ",").replace("٫", ".")
    lines = [re.sub(r"\s+", " ", line).strip() for line in normalized.splitlines() if line.strip()]
    header_text = "\n".join(lines[:8]).lower()
    leading_header_text = "\n".join(lines[:2]).lower()
    best_match = None
    best_score = 0

    for canonical_name, hints in BANK_HINTS:
        score = 0
        for hint in hints:
            normalized_hint = hint.lower()
            if normalized_hint in header_text:
                score += max(3, len(normalized_hint.split()))
            if normalized_hint in leading_header_text:
                score += 5

        if score > best_score:
            best_score = score
            best_match = canonical_name

    if best_match and best_score >= 3:
        return best_match

    return None


ARABIC_NUMERAL_TRANSLATION = str.maketrans("٠١٢٣٤٥٦٧٨٩", "0123456789")


def normalize_date_value(raw_value: str) -> str:
    sanitized = raw_value.translate(ARABIC_NUMERAL_TRANSLATION).replace("/", "-").replace("م", "").strip()
    parts = [part for part in re.split(r"\s*-\s*", sanitized) if part]
    if len(parts) != 3:
        return sanitized

    if len(parts[0]) == 4:
        year, month, day = parts
    else:
        day, month, year = parts

    return f"{int(year):04d}-{int(month):02d}-{int(day):02d}"


def find_contextual_value(regexes: list[re.Pattern], text: str) -> str | None:
    for regex in regexes:
        match = regex.search(text)
        if match:
            return match.group(1)
    return None


def clean_context_value(value: str | None) -> str | None:
    if not value:
        return None

    cleaned = re.sub(r"\s+", " ", value).strip(" :-\t\r\n")
    cleaned = re.split(r"\s{2,}|(?:\s*[\|•]\s*)", cleaned)[0].strip()
    cleaned = re.split(r"(?:بعد\s+التحية|حيث\s+أنكم|حيث\s+انكم|حيت\s+انكم|RFQ|PO#|PO\s|REF#|رقم\s+أمر|امر\s+شراء|purchase\s+order)", cleaned, maxsplit=1, flags=re.IGNORECASE)[0].strip(" ,:-")
    cleaned = re.sub(r"\b(?:المحترمين|الرياض|riyadh|trade finance center|مركز تمويل التجارة)\b", "", cleaned, flags=re.IGNORECASE)
    cleaned = re.sub(r"\s{2,}", " ", cleaned).strip(" ,:-")
    return cleaned or None


def extract_beneficiary_name(text: str) -> str | None:
    if re.search(r"(?:مستشفى|متشفي|مصتشفى|مستشفي)\s+الملك\s+فيصل", text, re.IGNORECASE):
        return "مستشفى الملك فيصل التخصصي ومركز الأبحاث"

    if (
        re.search(r"(?:مستشفى|متشفي|مصتشفى|مستشفي|مستفى)", text, re.IGNORECASE)
        and ("فيصل" in text or "فصل" in text or "فيمل" in text)
        and ("مركز" in text and ("الأبح" in text or "الابح" in text or "الابحث" in text))
    ):
        return "مستشفى الملك فيصل التخصصي ومركز الأبحاث"

    if "king faisal specialist hospital" in text.lower():
        return "King Faisal Specialist Hospital & Research Centre"

    return clean_context_value(find_contextual_value(BENEFICIARY_CONTEXT_REGEXES, text))


def extract_principal_name(text: str) -> str | None:
    patterns = [
        re.compile(r"(?:عملاءنا\s*السادة|عميلنا\s*السادة|عملائنا\s*السادة)\s*[:\-]?\s*(.+?)(?:\n|RFQ|PO#|PO\s|REF#|رقم\s+أمر|امر\s+شراء|purchase\s+order|بد\s+التحية|وذلك)", re.IGNORECASE),
        re.compile(r"(?:حيث\s+أنكم\s+منحتم\s+عملاءنا\s+السادة)\s*[:\-]?\s*(.+?)(?:\n|RFQ|PO#|PO\s|REF#|رقم\s+أمر|امر\s+شراء|purchase\s+order|وذلك)", re.IGNORECASE),
        re.compile(r"(?:اسم\s*(?:العميل|المقاول|المتقدم)|العميل|المقاول|المتقدم|principal|applicant|customer|contractor)\s*[:\-]?\s*(.+?)(?:\n|RFQ|PO#|PO\s|REF#|رقم\s+أمر|امر\s+شراء|purchase\s+order|وذلك)", re.IGNORECASE),
    ]

    for pattern in patterns:
        match = pattern.search(text)
        if not match:
            continue

        principal_name = clean_context_value(match.group(1))
        if principal_name and len(principal_name) <= 120:
            return principal_name

    return None


def build_page_text(recognized_regions: list[tuple[str, str]]) -> str:
    lines = []
    for recognized_text, _ in recognized_regions:
        cleaned = normalize_native_text(recognized_text.translate(ARABIC_NUMERAL_TRANSLATION))
        if cleaned:
            lines.append(cleaned)

    return "\n".join(lines)


def extract_contextual_amount(text: str) -> str | None:
    for regex in AMOUNT_CONTEXT_REGEXES:
        match = regex.search(text)
        if match:
            return match.group(1).replace(",", "")

    amount_candidates = []
    for line in text.splitlines():
        normalized_line = normalize_native_text(line)
        lower_line = normalized_line.lower()
        if not normalized_line:
            continue

        if any(marker in lower_line for marker in ["swift", "fax", "tel", "p.o", "po box", "cr ", "unit no"]):
            continue

        if not any(marker in lower_line for marker in ["مبلغ", "وقدره", "ريال", "sar", "amount", "value", "#"]):
            continue

        for match in AMOUNT_REGEX.finditer(normalized_line):
            raw_value = match.group(0).replace(",", "")
            try:
                numeric_value = float(raw_value)
            except ValueError:
                continue

            if numeric_value < 1000 or numeric_value > 100000000:
                continue

            amount_candidates.append((numeric_value, raw_value))

    if not amount_candidates:
        return None

    amount_candidates.sort(key=lambda item: item[0], reverse=True)
    return amount_candidates[0][1]


def extract_dates(text: str) -> list[str]:
    return [normalize_date_value(match.group(0)) for match in DATE_REGEX.finditer(text)]


def parse_arabic_number_phrase(text: str) -> int | None:
    normalized = normalize_arabic_phrase(text).replace("ة", "ه")
    if not normalized:
        return None

    direct = lookup_arabic_phrase_value(ARABIC_NUMBER_VALUE_MAP, normalized)
    if direct is not None:
        return direct

    normalized = re.sub(r"^و\s*", "", normalized).strip()
    direct = lookup_arabic_phrase_value(ARABIC_NUMBER_VALUE_MAP, normalized)
    if direct is not None:
        return direct

    parts = [part.strip() for part in normalized.split(" و ") if part.strip()]
    if len(parts) > 1:
        total = 0
        for part in parts:
            value = lookup_arabic_phrase_value(ARABIC_NUMBER_VALUE_MAP, part)
            if value is None:
                return None
            total += value
        return total

    compact = normalized.replace(" ", "")
    for tens_key, tens_value in ARABIC_NUMBER_VALUE_MAP.items():
        tens_compact = tens_key.replace(" ", "")
        for suffix in (tens_compact, f"و{tens_compact}"):
            if compact.endswith(suffix):
                prefix = compact[: -len(suffix)]
                if not prefix:
                    return tens_value

                prefix_value = lookup_arabic_phrase_value(ARABIC_NUMBER_VALUE_MAP, prefix)
                if prefix_value is not None:
                    return prefix_value + tens_value

    return None


def parse_arabic_year_phrase(text: str) -> int | None:
    text = re.sub(r"\s+للم\S+$", "", text or "").strip()
    normalized = normalize_arabic_phrase(text).replace("ة", "ه")
    if not normalized:
        return None

    if normalized in {"الفين", "الفان"}:
        return 2000

    if normalized.startswith("الفين "):
        suffix = normalized[len("الفين "):].strip()
        suffix = suffix.removeprefix("و ").strip()
        suffix_value = parse_arabic_number_phrase(suffix)
        return 2000 + suffix_value if suffix_value is not None else None

    if normalized.startswith("الفان "):
        suffix = normalized[len("الفان "):].strip()
        suffix = suffix.removeprefix("و ").strip()
        suffix_value = parse_arabic_number_phrase(suffix)
        return 2000 + suffix_value if suffix_value is not None else None

    return None


def parse_textual_expiry_date(text: str) -> str | None:
    normalized = normalize_arabic_phrase(text)
    match = TEXTUAL_EXPIRY_DATE_REGEX.search(normalized)
    if not match:
        return None

    day_phrase = match.group(1).strip()
    month_phrase = match.group(2).strip()
    year_phrase = match.group(3).strip()

    day = lookup_arabic_phrase_value(ARABIC_DAY_VALUE_MAP, day_phrase)
    month = lookup_arabic_phrase_value(ARABIC_MONTH_VALUE_MAP, month_phrase)
    year = parse_arabic_year_phrase(year_phrase)

    if day is None or month is None or year is None:
        return None

    return f"{year:04d}-{month:02d}-{day:02d}"


def build_structured_fields(
    text: str,
    request_payload: dict,
    page_number: int,
    source_label: str,
    bounding_box: str = "auto",
) -> list[dict]:
    normalized_text = normalize_native_text(text.translate(ARABIC_NUMERAL_TRANSLATION))
    fields: list[dict] = []

    guarantee_number = find_contextual_value(GUARANTEE_NUMBER_CONTEXT_REGEXES, normalized_text)
    if not guarantee_number:
        top_code_match = re.search(r"\b\d{3}\s+\d{6,8}\s+\d\b", normalized_text)
        if top_code_match:
            guarantee_number = re.sub(r"\s+", "", top_code_match.group(0))
    if not guarantee_number:
        guarantee_number = find_first([GUARANTEE_NUMBER_REGEX, COMPLEX_GUARANTEE_CODE_REGEX, GENERIC_GUARANTEE_CODE_REGEX], normalized_text)

    if guarantee_number:
        fields.append(
            make_field(
                "IntakeField_GuaranteeNumber",
                guarantee_number.upper(),
                99 if source_label == "direct-pdf-text" else 92,
                page_number,
                bounding_box,
                source_label,
            )
        )

    bank_name = looks_like_bank_name(text, request_payload)
    scenario_key = request_payload.get("scenarioKey", "") or ""
    if bank_name:
        fields.append(
            make_field(
                "IntakeField_BankName",
                bank_name,
                95 if source_label == "direct-pdf-text" else 88,
                page_number,
                bounding_box,
                source_label,
            )
        )

    beneficiary_name = extract_beneficiary_name(normalized_text)
    if beneficiary_name:
        fields.append(
            make_field(
                "IntakeField_Beneficiary",
                beneficiary_name,
                78 if source_label == "direct-pdf-text" else 70,
                page_number,
                bounding_box,
                source_label,
            )
        )

    principal_name = extract_principal_name(normalized_text)
    if principal_name:
        fields.append(
            make_field(
                "IntakeField_Principal",
                principal_name,
                76 if source_label == "direct-pdf-text" else 68,
                page_number,
                bounding_box,
                source_label,
            )
        )

    bank_reference = find_contextual_value(BANK_REFERENCE_CONTEXT_REGEXES, normalized_text)
    if not bank_reference:
        bank_reference = find_first(BANK_REFERENCE_REGEXES, normalized_text)

    if bank_reference:
        fields.append(
            make_field(
                "IntakeField_BankReference",
                bank_reference.upper(),
                94 if source_label == "direct-pdf-text" else 86,
                page_number,
                bounding_box,
                source_label,
            )
        )

    date_candidates = extract_dates(normalized_text)
    date_value = date_candidates[0] if date_candidates else find_contextual_value(OFFICIAL_DATE_CONTEXT_REGEXES, normalized_text)
    normalized_official_date = normalize_date_value(date_value) if date_value else None
    if date_value:
        date_field_key = "IntakeField_OfficialLetterDate" if scenario_key != "new-guarantee" else "IntakeField_IssueDate"
        fields.append(make_field(date_field_key, normalized_official_date, 85, page_number, bounding_box, source_label))

    amount_value = extract_contextual_amount(normalized_text)
    if amount_value and scenario_key in {"new-guarantee", "reduction-confirmation"}:
        fields.append(
            make_field(
                "IntakeField_Amount",
                amount_value.replace(",", ""),
                84 if source_label == "direct-pdf-text" else 80,
                page_number,
                bounding_box,
                source_label,
            )
        )

    currency_code_match = CURRENCY_CODE_REGEX.search(normalized_text)
    if currency_code_match and scenario_key == "new-guarantee":
        fields.append(
            make_field(
                "IntakeField_CurrencyCode",
                currency_code_match.group(0).upper(),
                83 if source_label == "direct-pdf-text" else 78,
                page_number,
                bounding_box,
                source_label,
            )
        )

    if scenario_key == "new-guarantee":
        if re.search(r"purchase\s*order|امر\s*شراء|\bpo\b", normalized_text, re.IGNORECASE):
            fields.append(
                make_field(
                    "IntakeField_GuaranteeCategory",
                    "PurchaseOrder",
                    72 if source_label == "direct-pdf-text" else 66,
                    page_number,
                    bounding_box,
                    source_label,
                )
            )
        elif re.search(r"\bcontract\b|عقد", normalized_text, re.IGNORECASE):
            fields.append(
                make_field(
                    "IntakeField_GuaranteeCategory",
                    "Contract",
                    72 if source_label == "direct-pdf-text" else 66,
                    page_number,
                    bounding_box,
                    source_label,
                )
            )

        expiry_date = find_contextual_value(EXPIRY_DATE_CONTEXT_REGEXES, normalized_text)
        if not expiry_date:
            expiry_date = parse_textual_expiry_date(normalized_text)
        if not expiry_date:
            later_dates = [
                candidate for candidate in date_candidates
                if normalized_official_date is None or candidate > normalized_official_date
            ]
            if later_dates:
                expiry_date = later_dates[-1]

        if expiry_date:
            fields.append(
                make_field(
                    "IntakeField_ExpiryDate",
                    normalize_date_value(expiry_date),
                    82 if source_label == "direct-pdf-text" else 76,
                    page_number,
                    bounding_box,
                    source_label,
                )
            )

    if scenario_key == "extension-confirmation":
        expiry_date = find_contextual_value(EXPIRY_DATE_CONTEXT_REGEXES, normalized_text)
        if not expiry_date:
            expiry_date = parse_textual_expiry_date(normalized_text)
        if not expiry_date:
            later_dates = [
                candidate for candidate in date_candidates
                if normalized_official_date is None or candidate > normalized_official_date
            ]
            if later_dates:
                expiry_date = later_dates[-1]

        if expiry_date:
            fields.append(
                make_field(
                    "IntakeField_NewExpiryDate",
                    normalize_date_value(expiry_date),
                    82 if source_label == "direct-pdf-text" else 76,
                    page_number,
                    bounding_box,
                    source_label,
                )
            )

    return fields


def make_field(field_key: str, value: str, confidence: int, page_number: int, bounding_box: str, source_label: str) -> dict:
    return {
        "fieldKey": field_key,
        "value": value,
        "confidencePercent": confidence,
        "pageNumber": page_number,
        "boundingBox": bounding_box,
        "sourceLabel": source_label,
    }


def process_text_first(document, request_payload: dict, page_numbers: list[int]) -> tuple[list[dict], list[str]]:
    warnings: list[str] = []
    page_texts, usable_page_count = extract_native_text(document, page_numbers)

    if usable_page_count == 0:
        return [], warnings

    fields: list[dict] = []
    for page_number, text in page_texts:
        if not text.strip():
            continue
        fields.extend(build_structured_fields(text, request_payload, page_number, "direct-pdf-text"))

    return deduplicate_fields(fields), warnings


def process_scanned(document, request_payload: dict, page_numbers: list[int], file_path: str) -> tuple[list[dict], list[str]]:
    warnings: list[str] = []
    fields: list[dict] = []
    direct_text_pages = 0
    scanned_pages = 0

    for page_number in page_numbers:
        page = document[page_number - 1]
        page_text = (page.get_text("text") or "").strip()
        if len(page_text) >= 20:
            direct_text_pages += 1
            fields.extend(build_structured_fields(page_text, request_payload, page_number, "direct-pdf-text"))
            continue

        scanned_pages += 1
        rendered_path = None
        preprocessed_path = None
        try:
            rendered_path, render_warnings = render_page(file_path, page, page_number)
            warnings.extend(render_warnings)
            if not rendered_path:
                continue

            preprocessed_path = os.path.join(tempfile.gettempdir(), f"bg-ocr-pre-{Path(rendered_path).name}")
            preprocessed_path, preprocess_warnings = preprocess_image(rendered_path)
            warnings.extend(preprocess_warnings)

            layout_regions, layout_warnings = detect_layout_regions(preprocessed_path)
            warnings.extend(layout_warnings)
            header_regions, header_warnings = extract_header_regions(preprocessed_path)
            warnings.extend(header_warnings)

            recognized_regions, ocr_warnings = run_ocr(preprocessed_path, layout_regions)
            warnings.extend(ocr_warnings)

            page_text = build_page_text(header_regions + recognized_regions)
            if page_text:
                fields.extend(
                    build_structured_fields(
                        page_text,
                        request_payload,
                        page_number,
                        "ocr-layout",
                        "page",
                    )
                )
        finally:
            cleanup_file(rendered_path)
            cleanup_file(preprocessed_path)

    if direct_text_pages > 0 and scanned_pages > 0:
        warnings.append("mixed-pdf-route")

    return deduplicate_fields(fields), deduplicate_warnings(warnings)


def cleanup_file(path: str) -> None:
    try:
        if path and os.path.exists(path):
            os.remove(path)
    except OSError:
        pass


def deduplicate_fields(fields: list[dict]) -> list[dict]:
    seen = set()
    output: list[dict] = []
    for field in fields:
        key = (field["fieldKey"], field["value"])
        if key in seen:
            continue
        seen.add(key)
        output.append(field)
    return output


def deduplicate_warnings(warnings: list[str]) -> list[str]:
    output: list[str] = []
    for warning in warnings:
        if warning not in output:
            output.append(warning)
    return output


def process_request(request_payload: dict) -> dict:
    file_path = request_payload.get("filePath", "") or ""
    warnings: list[str] = []

    if not file_path or not os.path.exists(file_path):
        return canonical_result(False, [], warnings, "ocr.file_not_found", "The OCR input file was not found.")

    max_file_size_bytes = os.environ.get("BG_OCR_MAX_FILE_SIZE_BYTES")
    if max_file_size_bytes:
        try:
            max_file_size = int(max_file_size_bytes)
        except ValueError:
            max_file_size = 0

        if max_file_size > 0:
            actual_size = os.path.getsize(file_path)
            if actual_size > max_file_size:
                return canonical_result(
                    False,
                    [],
                    warnings,
                    "ocr.file_too_large",
                    "The OCR input file exceeded the configured size limit.")

    if fitz is None:
        return canonical_result(False, [], warnings, "ocr.pymupdf_missing", "PyMuPDF is not installed.")

    document = open_document(file_path)
    try:
        page_numbers = resolve_selected_page_numbers(document, request_payload)
        page_texts, usable_page_count = extract_native_text(document, page_numbers)
        fields = []
        if usable_page_count == len(page_texts) and usable_page_count > 0:
            fields, text_warnings = process_text_first(document, request_payload, page_numbers)
            warnings.extend(text_warnings)
            return canonical_result(True, fields, deduplicate_warnings(warnings), pipeline_version="wave2-text-first")

        scan_fields, scan_warnings = process_scanned(document, request_payload, page_numbers, file_path)
        warnings.extend(scan_warnings)

        pipeline_version = "wave2-scan-route"
        if "mixed-pdf-route" in warnings:
            pipeline_version = "wave2-mixed-route"

        return canonical_result(True, scan_fields, deduplicate_warnings(warnings), pipeline_version=pipeline_version)
    finally:
        document.close()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--request", required=True)
    args = parser.parse_args()

    request_path = Path(args.request)
    if not request_path.exists():
        print(json.dumps(canonical_result(False, [], [], "ocr.request_not_found", "The OCR worker request file was not found.")))
        return 1

    try:
        request_payload = read_request(request_path)
        response = process_request(request_payload)
    except Exception as exception:
        traceback.print_exc(file=sys.stderr)
        response = canonical_result(
            False,
            [],
            ["ocr-worker-exception"],
            "ocr.worker_exception",
            str(exception))

    print(json.dumps(response, ensure_ascii=False))
    return 0 if response.get("succeeded") else 1


if __name__ == "__main__":
    raise SystemExit(main())
