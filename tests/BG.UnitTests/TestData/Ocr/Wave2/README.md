# Wave 2 OCR Reality Set

This folder contains the second OCR evaluation wave built from the real
multi-page scanned bundles:

- `extension.pdf`
- `attachments.pdf`

This wave is intentionally different from `Wave1`:

- it covers mixed banks and mixed document families
- it is used to measure extraction quality, not only smoke coverage
- it includes both original guarantees and amendment / extension pages

Usage rule:

- treat every `*.expected.json` file here as manually validated truth for the
  selected page only
- use `scripts/evaluate_ocr_wave.py` to compute the current score
- do not promote this wave into the default CI gate until the score becomes
  stable enough to avoid noisy failures

The goal of `Wave2` is to stop relying on impressions and start measuring:

- field completeness
- exact-match correctness
- regression risk on real scanned bank documents
