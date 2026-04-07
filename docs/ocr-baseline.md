# OCR Baseline Runbook

This runbook executes a measurable OCR baseline using real guarantee documents.

## 1) Prepare input files

1. Copy sample PDFs to data/documents.
2. Fill ground truth rows in data/ocr_ground_truth_baseline.csv.
3. Keep field values in canonical format when possible:
   - Dates: YYYY-MM-DD
   - Amount: numeric only (no commas preferred)
   - Guarantee number: normalized final value

## 2) Run baseline evaluation

From repository root:

```powershell
.venv-ocr312\Scripts\python.exe scripts\evaluate_baseline.py
```

Optional parameters:

```powershell
.venv-ocr312\Scripts\python.exe scripts\evaluate_baseline.py `
  --ground-truth data\ocr_ground_truth_baseline.csv `
  --documents-root data\documents `
  --worker-script src\BG.Integrations\OcrWorker\ocr_worker.py `
  --output-json data\ocr_baseline_results.json `
  --output-csv data\ocr_baseline_results.csv
```

## 3) Outputs

- data/ocr_baseline_results.json
- data/ocr_baseline_results.csv

Key metrics:
- overallAccuracyPercent
- medianLatencyMs
- p95LatencyMs
- fieldAccuracy per IntakeField key

## 4) Notes

- The script runs the same OCR worker used by the application.
- A document row fails if the worker fails or the file is missing.
- Return code is non-zero when one or more rows fail.
