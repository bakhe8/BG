import argparse
import json
import sys
from collections import defaultdict
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


def canonical_bank_name(bank_profile_key: str) -> str | None:
    return {
        "riyad": "Riyad Bank",
        "snb": "Saudi National Bank",
        "alrajhi": "Al Rajhi Bank",
        "alinma": "Alinma Bank",
        "bnp-paribas": "BNP Paribas",
        "anb": "Arab National Bank",
        "bsf": "Banque Saudi Fransi",
        "sabb": "SABB",
        "saib": "Saudi Investment Bank",
    }.get((bank_profile_key or "").strip().lower())


def extract_page(worker, pdf_path: Path, page_definition: dict) -> dict:
    document = fitz.open(pdf_path)
    page_number = page_definition["page"]
    page = document[page_number - 1]
    request = {
        "scenarioKey": page_definition["scenarioKey"],
        "documentFormKey": page_definition["documentFormKey"],
        "bankProfileKey": page_definition["bankProfileKey"],
        "structuralClassKey": page_definition["structuralClassKey"],
        "canonicalBankName": canonical_bank_name(page_definition["bankProfileKey"]),
    }

    rendered_path = None
    preprocessed_path = None
    warnings: list[str] = []

    try:
        page_text = (page.get_text("text") or "").strip()
        if len(page_text) >= 20:
            pipeline_version = "wave2-text-first"
            fields = worker.build_structured_fields(
                page_text,
                request,
                page_number,
                "direct-pdf-text",
                "page")
        else:
            pipeline_version = "wave2-scan-route"
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

        fields = worker.deduplicate_fields(fields)
        return {
            "pageNumber": page_number,
            "pipelineVersion": pipeline_version,
            "warnings": worker.deduplicate_warnings(warnings),
            "fields": fields,
            "record": normalize_record(fields),
        }
    finally:
        worker.cleanup_file(rendered_path)
        worker.cleanup_file(preprocessed_path)


def load_expected_truth(expected_path: Path) -> dict:
    return json.loads(expected_path.read_text(encoding="utf-8-sig"))


def compare_page(expected_truth: dict, actual_result: dict) -> dict:
    fields = []
    matched = 0
    total = 0

    expected_pipeline = expected_truth["pipelineVersion"]
    actual_pipeline = actual_result["pipelineVersion"]
    pipeline_matched = expected_pipeline == actual_pipeline

    if pipeline_matched:
        matched += 1
    total += 1

    actual_record = actual_result["record"]
    for expected_field in expected_truth["requiredFields"]:
        field_key = expected_field["fieldKey"]
        expected_value = expected_field["value"]
        actual_value = actual_record.get(field_key)
        ok = actual_value == expected_value
        if ok:
            matched += 1
        total += 1
        fields.append(
            {
                "fieldKey": field_key,
                "expected": expected_value,
                "actual": actual_value,
                "matched": ok,
            }
        )

    return {
        "pipeline": {
            "expected": expected_pipeline,
            "actual": actual_pipeline,
            "matched": pipeline_matched,
        },
        "fields": fields,
        "matched": matched,
        "total": total,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--wave-dir", required=True)
    parser.add_argument("--output")
    parser.add_argument("--fail-on-mismatch", action="store_true")
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parents[1]
    wave_dir = Path(args.wave_dir).resolve()
    output_path = Path(args.output).resolve() if args.output else (repo_root / ".artifacts" / "ocr-wave-evaluation" / wave_dir.name / "comparison.json")
    output_path.parent.mkdir(parents=True, exist_ok=True)

    manifest = json.loads((wave_dir / "manifest.json").read_text(encoding="utf-8"))
    worker = load_worker(repo_root)

    pages_by_file: dict[str, list[dict]] = defaultdict(list)
    for page in manifest["pages"]:
        pages_by_file[page["file"]].append(page)

    actual_by_file: dict[str, list[dict]] = {}
    comparisons = []
    matched = 0
    total = 0
    passed_pages = 0

    for file_name, pages in pages_by_file.items():
        pdf_path = wave_dir / file_name
        actual_by_file[file_name] = []

        for page in pages:
            actual_result = extract_page(worker, pdf_path, page)
            actual_by_file[file_name].append(actual_result)

            expected_path = wave_dir / page["expectedTruthFile"]
            expected_truth = load_expected_truth(expected_path)
            comparison = compare_page(expected_truth, actual_result)

            matched += comparison["matched"]
            total += comparison["total"]
            if comparison["matched"] == comparison["total"]:
                passed_pages += 1

            comparisons.append(
                {
                    "file": file_name,
                    "page": page["page"],
                    "scenarioKey": page["scenarioKey"],
                    "expectation": page["expectation"],
                    "comparison": comparison,
                    "actualRecord": actual_result["record"],
                    "warnings": actual_result["warnings"],
                }
            )

    summary = {
        "wave": manifest["wave"],
        "purpose": manifest["purpose"],
        "matched": matched,
        "total": total,
        "accuracyPercent": round((matched / total) * 100, 2) if total else 0.0,
        "passedPages": passed_pages,
        "totalPages": len(manifest["pages"]),
        "pages": comparisons,
        "actual": actual_by_file,
    }
    output_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(
        json.dumps(
            {
                "wave": summary["wave"],
                "comparison": str(output_path),
                "accuracyPercent": summary["accuracyPercent"],
                "matched": summary["matched"],
                "total": summary["total"],
                "passedPages": summary["passedPages"],
                "totalPages": summary["totalPages"],
            },
            ensure_ascii=False,
        )
    )

    if args.fail_on_mismatch and matched != total:
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
