#!/usr/bin/env python3
"""Bootstrap OCR ground-truth CSV rows from current worker output.

This creates a seed file so baseline execution can run end-to-end immediately.
Values are auto-generated predictions and should be reviewed later.
"""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import tempfile
import time
from pathlib import Path

FIELDS = [
    "IntakeField_GuaranteeNumber",
    "IntakeField_BankName",
    "IntakeField_Beneficiary",
    "IntakeField_Principal",
    "IntakeField_BankReference",
    "IntakeField_OfficialLetterDate",
    "IntakeField_IssueDate",
    "IntakeField_Amount",
    "IntakeField_CurrencyCode",
    "IntakeField_GuaranteeCategory",
    "IntakeField_ExpiryDate",
    "IntakeField_NewExpiryDate",
]

HEADER = [
    "id",
    "documentPath",
    "scenarioKey",
    "documentFormKey",
    "bankProfileKey",
    "structuralClassKey",
    "canonicalBankName",
    "referencePrefix",
    *FIELDS,
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Seed OCR baseline CSV from worker predictions")
    parser.add_argument("--documents-root", default="data/documents")
    parser.add_argument("--python-exe", default=".venv-ocr312/Scripts/python.exe")
    parser.add_argument("--worker-script", default="src/BG.Integrations/OcrWorker/ocr_worker.py")
    parser.add_argument("--output-csv", default="data/ocr_ground_truth_baseline.csv")
    return parser.parse_args()


def parse_worker_output(raw: str) -> dict:
    raw = (raw or "").strip()
    if not raw:
        return {}
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        for line in reversed(raw.splitlines()):
            line = line.strip()
            if line.startswith("{"):
                try:
                    return json.loads(line)
                except json.JSONDecodeError:
                    pass
    return {}


def best_fields(fields: list[dict]) -> dict[str, str]:
    grouped: dict[str, dict] = {}
    for f in fields:
        key = str(f.get("fieldKey") or "").strip()
        if not key:
            continue
        if key not in grouped or int(f.get("confidencePercent") or 0) > int(grouped[key].get("confidencePercent") or 0):
            grouped[key] = f
    return {k: str(v.get("value") or "") for k, v in grouped.items()}


def run_worker(python_exe: Path, worker_script: Path, document_path: Path) -> dict[str, str]:
    request_payload = {
        "stagedDocumentToken": f"seed-{int(time.time() * 1000)}",
        "filePath": str(document_path.resolve()),
        "originalFileName": document_path.name,
        "scenarioKey": "new-guarantee",
        "documentFormKey": "GuaranteeInstrument_Generic",
        "bankProfileKey": "BankProfile_Generic",
        "structuralClassKey": "StructuralClass_Generic",
        "canonicalBankName": None,
        "referencePrefix": "BG",
        "selectedPages": None,
    }

    with tempfile.NamedTemporaryFile("w", suffix=".json", encoding="utf-8", delete=False) as tmp:
        req_path = Path(tmp.name)
        json.dump(request_payload, tmp, ensure_ascii=False)

    try:
        completed = subprocess.run(
            [str(python_exe), str(worker_script), "--request", str(req_path)],
            capture_output=True,
            text=True,
            encoding="utf-8",
        )
    finally:
        req_path.unlink(missing_ok=True)

    output = parse_worker_output(completed.stdout)
    if completed.returncode != 0 or not output.get("succeeded", False):
        return {}
    return best_fields(output.get("fields") or [])


def collect_pdfs(root: Path) -> list[Path]:
    return sorted([p for p in root.rglob("*.pdf") if p.is_file()])


def main() -> int:
    args = parse_args()
    documents_root = Path(args.documents_root).resolve()
    python_exe = Path(args.python_exe).resolve()
    worker_script = Path(args.worker_script).resolve()
    output_csv = Path(args.output_csv).resolve()

    pdfs = collect_pdfs(documents_root)
    if not pdfs:
        print(f"No PDF files found under: {documents_root}")
        return 2

    rows: list[dict[str, str]] = []
    for i, pdf in enumerate(pdfs, start=1):
        predicted = run_worker(python_exe, worker_script, pdf)
        relative_path = pdf.relative_to(documents_root).as_posix()

        row = {
            "id": f"auto-{i:04d}",
            "documentPath": relative_path,
            "scenarioKey": "new-guarantee",
            "documentFormKey": "GuaranteeInstrument_Generic",
            "bankProfileKey": "BankProfile_Generic",
            "structuralClassKey": "StructuralClass_Generic",
            "canonicalBankName": predicted.get("IntakeField_BankName", ""),
            "referencePrefix": "BG",
        }
        for field in FIELDS:
            row[field] = predicted.get(field, "")

        rows.append(row)
        print(f"Seeded {i}/{len(pdfs)}: {relative_path}")

    output_csv.parent.mkdir(parents=True, exist_ok=True)
    with output_csv.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=HEADER)
        writer.writeheader()
        writer.writerows(rows)

    print(f"Wrote seeded baseline CSV: {output_csv}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
