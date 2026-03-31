# Wave 1 OCR Samples

This folder contains the temporary `Wave 1` scanned bank-document samples used
to anchor the first OCR implementation pass.

Current contents:

- `45-Attachment.pdf`
- `Attachments.pdf`
- `45-Attachment-page35.pdf`
- `Attachments-page02.pdf`
- `manifest.json`
- `45-Attachment-page33.expected.json`
- `45-Attachment-page35.expected.json`
- `45-Attachment-page37.expected.json`
- `45-Attachment-page39.expected.json`
- `45-Attachment-page47.expected.json`
- `Attachments-page02.expected.json`

These files are treated as:

- the first scanned-PDF OCR baseline
- temporary in-project regression material
- non-final sample coverage to be expanded later
- plus two small subset PDFs used for fast smoke coverage

Preferred regression rule:

- use `manifest.json` page selections against the original files for default OCR smoke tests
- keep the one-page subset PDFs as convenience assets for manual checks only
- use the `*.expected.json` files as truth baselines for page-scoped regression checks

Current page-scoped regression coverage:

- `45-Attachment.pdf`
  - page `33` (`Riyad`)
  - page `35` (`Al Rajhi`)
  - page `37` (`Alinma`)
  - page `39` (`Riyad`)
  - page `47` (`Riyad`)
- `Attachments.pdf`
  - page `2` (`BSF`, scanned route)

Important:

- this folder is intentionally temporary and operational
- it does not represent the final bank-form corpus
- future waves should be added without changing the OCR integration contract
