#!/usr/bin/env python3
"""Evaluate OCR extraction baseline against a ground-truth CSV.

This script executes the existing OCR worker (optionally in parallel),
then compares extracted field values to expected values from CSV.

Features:
  --workers N    Run N documents concurrently (default 4)
  --cache-dir    SHA256-keyed result cache — re-runs are near-instant
  --no-cache     Force fresh OCR even if cache exists
"""

from __future__ import annotations

import argparse
import concurrent.futures
import csv
import hashlib
import json
import os
import statistics
import subprocess
import sys
import tempfile
import threading
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any


DEFAULT_FIELDS = [
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

_print_lock = threading.Lock()


def log(msg: str) -> None:
    with _print_lock:
        print(msg, flush=True)


@dataclass
class DocumentResult:
    row_id: str
    document_path: str
    succeeded: bool
    elapsed_ms: int
    warning_count: int
    matched: int
    expected: int
    cache_hit: bool = False
    error: str | None = None


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Evaluate OCR baseline against ground truth")
    parser.add_argument("--ground-truth", default="data/ocr_ground_truth_baseline.csv")
    parser.add_argument("--documents-root", default="data/documents")
    parser.add_argument(
        "--python-exe",
        default=".venv-ocr312/Scripts/python.exe" if os.name == "nt" else "python3",
    )
    parser.add_argument("--worker-script", default="src/BG.Integrations/OcrWorker/ocr_worker.py")
    parser.add_argument("--output-json", default="data/ocr_baseline_results.json")
    parser.add_argument("--output-csv", default="data/ocr_baseline_results.csv")
    parser.add_argument("--workers", type=int, default=4, help="Parallel worker count (default 4)")
    parser.add_argument("--cache-dir", default="data/ocr_result_cache", help="Result cache directory")
    parser.add_argument("--no-cache", action="store_true", help="Ignore existing cache entries")
    parser.add_argument("--fail-on-worker-error", action="store_true")
    return parser.parse_args()


def norm_value(value: str) -> str:
    cleaned = (value or "").strip()
    return " ".join(cleaned.split()).lower()


def compute_file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def load_cached_output(cache_dir: Path, doc_path: Path) -> dict | None:
    try:
        sha = compute_file_sha256(doc_path)
        cache_file = cache_dir / f"{sha}.json"
        if cache_file.exists():
            return json.loads(cache_file.read_text("utf-8"))
    except Exception:
        pass
    return None


def save_cached_output(cache_dir: Path, doc_path: Path, output: dict) -> None:
    try:
        sha = compute_file_sha256(doc_path)
        cache_dir.mkdir(parents=True, exist_ok=True)
        cache_file = cache_dir / f"{sha}.json"
        cache_file.write_text(json.dumps(output, ensure_ascii=False), "utf-8")
    except Exception:
        pass


def parse_worker_output(raw_stdout: str) -> dict[str, Any]:
    if not raw_stdout.strip():
        raise ValueError("empty worker output")
    try:
        return json.loads(raw_stdout)
    except json.JSONDecodeError:
        for line in reversed(raw_stdout.splitlines()):
            line = line.strip()
            if not line.startswith("{"):
                continue
            try:
                return json.loads(line)
            except json.JSONDecodeError:
                continue
        raise ValueError("worker output is not valid JSON")


def select_best_field_values(fields: list[dict[str, Any]]) -> dict[str, str]:
    grouped: dict[str, list[dict[str, Any]]] = {}
    for field in fields:
        key = str(field.get("fieldKey") or "").strip()
        if not key:
            continue
        grouped.setdefault(key, []).append(field)

    selected: dict[str, str] = {}
    for key, candidates in grouped.items():
        best = max(candidates, key=lambda item: int(item.get("confidencePercent") or 0))
        selected[key] = str(best.get("value") or "")
    return selected


def build_request_payload(row: dict[str, str], absolute_document_path: Path) -> dict[str, Any]:
    return {
        "stagedDocumentToken": f"baseline-{int(time.time() * 1000)}",
        "filePath": str(absolute_document_path),
        "originalFileName": absolute_document_path.name,
        "scenarioKey": (row.get("scenarioKey") or "new-guarantee").strip(),
        "documentFormKey": (row.get("documentFormKey") or "").strip(),
        "bankProfileKey": (row.get("bankProfileKey") or "").strip(),
        "structuralClassKey": (row.get("structuralClassKey") or "").strip(),
        "canonicalBankName": (row.get("canonicalBankName") or "").strip() or None,
        "referencePrefix": (row.get("referencePrefix") or "").strip() or None,
        "selectedPages": None,
    }


def evaluate_row(
    row: dict[str, str],
    documents_root: Path,
    python_exe: Path,
    worker_script: Path,
    fail_on_worker_error: bool,
    cache_dir: Path | None,
) -> tuple[DocumentResult, dict[str, str], dict[str, str]]:
    row_id = (row.get("id") or "").strip() or "(no-id)"
    relative_path = (row.get("documentPath") or "").strip()
    if not relative_path:
        raise ValueError(f"row {row_id}: documentPath is required")

    absolute_path = Path(relative_path)
    if not absolute_path.is_absolute():
        absolute_path = (documents_root / relative_path).resolve()

    if not absolute_path.exists():
        return (
            DocumentResult(row_id=row_id, document_path=str(absolute_path),
                           succeeded=False, elapsed_ms=0, warning_count=0,
                           matched=0, expected=0, error="document-not-found"),
            {}, {},
        )

    expected_values = {
        field: (row.get(field) or "").strip()
        for field in DEFAULT_FIELDS
        if (row.get(field) or "").strip()
    }

    # --- Cache check ---
    cache_hit = False
    output: dict[str, Any] | None = None
    if cache_dir:
        output = load_cached_output(cache_dir, absolute_path)
        if output is not None:
            cache_hit = True

    # --- Run worker if no cache ---
    elapsed_ms = 0
    if output is None:
        request_payload = build_request_payload(row, absolute_path)
        with tempfile.NamedTemporaryFile("w", suffix=".json", encoding="utf-8", delete=False) as fh:
            request_file = Path(fh.name)
            json.dump(request_payload, fh, ensure_ascii=False)

        started = time.perf_counter()
        completed = subprocess.run(
            [str(python_exe), str(worker_script), "--request", str(request_file)],
            capture_output=True, text=True, encoding="utf-8",
        )
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        request_file.unlink(missing_ok=True)

        try:
            output = parse_worker_output(completed.stdout)
        except Exception as exc:
            if fail_on_worker_error:
                raise
            return (
                DocumentResult(row_id=row_id, document_path=str(absolute_path),
                               succeeded=False, elapsed_ms=elapsed_ms, warning_count=0,
                               matched=0, expected=len(expected_values),
                               error=f"invalid-worker-output: {exc}"),
                {}, expected_values,
            )

        if completed.returncode != 0 or not output.get("succeeded", False):
            error = output.get("errorCode") or "worker-failed"
            if fail_on_worker_error:
                raise RuntimeError(f"row {row_id}: OCR worker failed with error {error}")
            return (
                DocumentResult(row_id=row_id, document_path=str(absolute_path),
                               succeeded=False, elapsed_ms=elapsed_ms,
                               warning_count=len(output.get("warnings") or []),
                               matched=0, expected=len(expected_values), error=str(error)),
                {}, expected_values,
            )

        if cache_dir:
            save_cached_output(cache_dir, absolute_path, output)

    predicted = select_best_field_values(output.get("fields") or output.get("fieldCandidates") or [])

    matched = sum(
        1 for fk, ev in expected_values.items()
        if norm_value(ev) == norm_value(predicted.get(fk, ""))
    )

    return (
        DocumentResult(row_id=row_id, document_path=str(absolute_path),
                       succeeded=True, elapsed_ms=elapsed_ms,
                       warning_count=len(output.get("warnings") or []),
                       matched=matched, expected=len(expected_values),
                       cache_hit=cache_hit),
        predicted, expected_values,
    )


def main() -> int:
    args = parse_args()

    ground_truth_path = Path(args.ground_truth).resolve()
    documents_root    = Path(args.documents_root).resolve()
    python_exe        = Path(args.python_exe).resolve()
    worker_script     = Path(args.worker_script).resolve()
    output_json_path  = Path(args.output_json).resolve()
    output_csv_path   = Path(args.output_csv).resolve()
    cache_dir         = None if args.no_cache else Path(args.cache_dir).resolve()

    for label, p in [("Ground truth CSV", ground_truth_path),
                     ("Python executable", python_exe),
                     ("Worker script", worker_script)]:
        if not p.exists():
            print(f"{label} not found: {p}", file=sys.stderr)
            return 2

    rows: list[dict[str, str]] = []
    with ground_truth_path.open("r", encoding="utf-8-sig", newline="") as fh:
        reader = csv.DictReader(fh)
        rows.extend(dict(row) for row in reader)

    if not rows:
        print("Ground truth CSV has no data rows", file=sys.stderr)
        return 2

    ordered_results: list[tuple[DocumentResult, dict[str, str], dict[str, str]]] = [None] * len(rows)  # type: ignore

    def process(idx_row: tuple[int, dict[str, str]]) -> tuple[int, DocumentResult, dict, dict]:
        idx, row = idx_row
        result, predicted, expected = evaluate_row(
            row, documents_root, python_exe, worker_script,
            args.fail_on_worker_error, cache_dir,
        )
        tag = "CACHE" if result.cache_hit else ("OK" if result.succeeded else "FAIL")
        log(
            f"[{tag}] {result.row_id} | matched {result.matched}/{result.expected} | "
            f"{result.elapsed_ms} ms | {Path(result.document_path).name}"
        )
        return idx, result, predicted, expected

    n_workers = max(1, args.workers)
    with concurrent.futures.ThreadPoolExecutor(max_workers=n_workers) as executor:
        for idx, result, predicted, expected in executor.map(process, enumerate(rows)):
            ordered_results[idx] = (result, predicted, expected)

    document_results: list[DocumentResult] = []
    field_stats: dict[str, dict[str, int]] = {k: {"matched": 0, "expected": 0} for k in DEFAULT_FIELDS}

    for result, predicted, expected in ordered_results:
        document_results.append(result)
        if result.succeeded:
            for fk in DEFAULT_FIELDS:
                ev = expected.get(fk, "")
                if not ev:
                    continue
                field_stats[fk]["expected"] += 1
                if norm_value(ev) == norm_value(predicted.get(fk, "")):
                    field_stats[fk]["matched"] += 1

    succeeded_docs = [r for r in document_results if r.succeeded]
    failed_docs    = [r for r in document_results if not r.succeeded]
    cache_hits     = sum(1 for r in document_results if r.cache_hit)

    total_expected = sum(r.expected for r in succeeded_docs)
    total_matched  = sum(r.matched  for r in succeeded_docs)
    overall_accuracy = (total_matched / total_expected * 100.0) if total_expected else 0.0

    latencies = [r.elapsed_ms for r in succeeded_docs]
    median_ms = int(statistics.median(latencies)) if latencies else 0
    p95_ms = 0
    if latencies:
        sorted_times = sorted(latencies)
        p95_ms = sorted_times[max(0, int(len(sorted_times) * 0.95) - 1)]

    field_accuracy = {
        fk: {
            "matched": s["matched"],
            "expected": s["expected"],
            "accuracyPercent": round(s["matched"] / s["expected"] * 100.0, 2) if s["expected"] else None,
        }
        for fk, s in field_stats.items()
    }

    summary = {
        "generatedAtUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "groundTruthPath": str(ground_truth_path),
        "documentsRoot": str(documents_root),
        "workerScript": str(worker_script),
        "pythonExecutable": str(python_exe),
        "documentsTotal": len(document_results),
        "documentsSucceeded": len(succeeded_docs),
        "documentsFailed": len(failed_docs),
        "cacheHits": cache_hits,
        "overallAccuracyPercent": round(overall_accuracy, 2),
        "medianLatencyMs": median_ms,
        "p95LatencyMs": p95_ms,
        "fieldAccuracy": field_accuracy,
        "failedRows": [{"id": r.row_id, "documentPath": r.document_path, "error": r.error} for r in failed_docs],
    }

    output_json_path.parent.mkdir(parents=True, exist_ok=True)
    output_json_path.write_text(json.dumps(summary, indent=2, ensure_ascii=False), "utf-8")

    output_csv_path.parent.mkdir(parents=True, exist_ok=True)
    with output_csv_path.open("w", encoding="utf-8", newline="") as fh:
        writer = csv.writer(fh)
        writer.writerow(["id", "documentPath", "status", "matched", "expected", "elapsedMs", "cacheHit", "warningCount", "error"])
        for r in document_results:
            writer.writerow([r.row_id, r.document_path, "ok" if r.succeeded else "failed",
                             r.matched, r.expected, r.elapsed_ms,
                             "yes" if r.cache_hit else "no", r.warning_count, r.error or ""])

    print(f"\nSummary")
    print(f"  Documents : {len(document_results)}")
    print(f"  Succeeded : {len(succeeded_docs)}")
    print(f"  Failed    : {len(failed_docs)}")
    print(f"  Cache hits: {cache_hits}/{len(document_results)}")
    print(f"  Accuracy  : {summary['overallAccuracyPercent']}%")
    print(f"  Median    : {median_ms} ms")
    print(f"  P95       : {p95_ms} ms")
    print(f"  JSON      : {output_json_path}")
    print(f"  CSV       : {output_csv_path}")

    return 0 if not failed_docs else 1


if __name__ == "__main__":
    raise SystemExit(main())
