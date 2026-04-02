import argparse
import json
import sys
from pathlib import Path

import fitz


def load_worker(repo_root: Path):
    sys.path.insert(0, str(repo_root / "src" / "BG.Integrations" / "OcrWorker"))
    import ocr_worker  # type: ignore

    return ocr_worker


def normalize_record(fields: list[dict]) -> dict:
    record: dict[str, str] = {}
    for field in fields:
        key = field.get("fieldKey")
        value = (field.get("value") or "").strip()
        if not key or not value:
            continue
        if key not in record:
            record[key] = value

    return record


def extract_pdf(repo_root: Path, pdf_path: Path, scenario_key: str, output_path: Path) -> list[dict]:
    worker = load_worker(repo_root)
    document = fitz.open(pdf_path)
    request = {
        "scenarioKey": scenario_key,
        "canonicalBankName": None,
    }

    results: list[dict] = []
    output_path.parent.mkdir(parents=True, exist_ok=True)

    for page_number in range(1, document.page_count + 1):
        page = document[page_number - 1]
        rendered_path = None
        preprocessed_path = None
        warnings: list[str] = []

        try:
            page_text = (page.get_text("text") or "").strip()
            if len(page_text) >= 20:
                fields = worker.build_structured_fields(
                    page_text,
                    request,
                    page_number,
                    "direct-pdf-text",
                    "page")
            else:
                rendered_path, render_warnings = worker.render_page(str(pdf_path), page, page_number)
                warnings.extend(render_warnings)

                if not rendered_path:
                    fields = []
                else:
                    preprocessed_path, preprocess_warnings = worker.preprocess_image(rendered_path)
                    warnings.extend(preprocess_warnings)
                    layout_regions, layout_warnings = worker.detect_layout_regions(preprocessed_path)
                    warnings.extend(layout_warnings)
                    header_regions, header_warnings = worker.extract_header_regions(preprocessed_path)
                    warnings.extend(header_warnings)
                    recognized_regions, ocr_warnings = worker.run_ocr(preprocessed_path, layout_regions)
                    warnings.extend(ocr_warnings)
                    merged_page_text = worker.build_page_text(header_regions + recognized_regions)
                    fields = worker.build_structured_fields(
                        merged_page_text,
                        request,
                        page_number,
                        "ocr-layout",
                        "page")

            page_result = {
                "pageNumber": page_number,
                "warnings": worker.deduplicate_warnings(warnings),
                "fields": worker.deduplicate_fields(fields),
                "record": normalize_record(worker.deduplicate_fields(fields)),
            }
            results.append(page_result)
            output_path.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
        finally:
            worker.cleanup_file(rendered_path)
            worker.cleanup_file(preprocessed_path)

    return results


def compare(expected_path: Path, actual_results: list[dict]) -> dict:
    expected = json.loads(expected_path.read_text(encoding="utf-8-sig"))
    actual_by_page = {item["pageNumber"]: item["record"] for item in actual_results}
    comparison = []
    matched = 0
    total = 0

    for expected_item in expected:
        page_number = expected_item["pageNumber"]
        actual = actual_by_page.get(page_number, {})
        fields = []
        for field_key, expected_value in expected_item["expected"].items():
            total += 1
            actual_value = actual.get(field_key)
            ok = actual_value == expected_value
            if ok:
                matched += 1
            fields.append({
                "fieldKey": field_key,
                "expected": expected_value,
                "actual": actual_value,
                "matched": ok,
            })

        comparison.append({
            "pageNumber": page_number,
            "fields": fields,
        })

    return {
        "matched": matched,
        "total": total,
        "accuracyPercent": round((matched / total) * 100, 2) if total else 0.0,
        "pages": comparison,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--pdf", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--scenario", default="new-guarantee")
    parser.add_argument("--expected")
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parents[1]
    pdf_path = Path(args.pdf).resolve()
    output_path = Path(args.output).resolve()

    results = extract_pdf(repo_root, pdf_path, args.scenario, output_path)

    if args.expected:
        expected_path = Path(args.expected).resolve()
        comparison = compare(expected_path, results)
        comparison_path = output_path.with_name(f"{output_path.stem}.comparison.json")
        comparison_path.write_text(json.dumps(comparison, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({
            "output": str(output_path),
            "comparison": str(comparison_path),
            "accuracyPercent": comparison["accuracyPercent"],
            "matched": comparison["matched"],
            "total": comparison["total"],
        }, ensure_ascii=False))
    else:
        print(json.dumps({
            "output": str(output_path),
            "pages": len(results),
        }, ensure_ascii=False))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
