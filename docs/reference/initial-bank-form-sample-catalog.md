# Initial Bank Form Sample Catalog

## Purpose

This document records the first real scanned bank guarantee samples received for
BG. These files are now the initial reality baseline for:

- intake form detection
- OCR/provider prioritization
- document-family catalog growth
- downstream operational validation

## Source Files

- `C:\Users\Bakheet\OneDrive\Desktop\10-3-2026\sally mail\45-Attachment.pdf`
- `C:\Users\Bakheet\OneDrive\Desktop\10-3-2026\Mohammad email\Attachments.pdf`

## Important Observation

Both source files are image-based scanned PDFs with no extractable text layer.

That means:

- OCR is confirmed as the first external integration priority
- text-only PDF parsing is not sufficient for real intake material
- model recognition must tolerate scan noise, stamps, signatures, and mixed
  Arabic/English layouts

## Observed Bank Families

The following bank document families are clearly present in the two sample
files:

- `Alinma Bank / مصرف الإنماء`
- `Riyad Bank`
- `BNP Paribas`
- `ANB / Arab National Bank`
- `BSF / Banque Saudi Fransi`
- `SABB`
- `SNB / Saudi National Bank / الأهلي`
- `Saudi Investment Bank`

## Observed Document Shapes

The samples show that BG must support more than one legal document shape for
the same business intent.

### Original Guarantee / Original Instrument

Observed examples include:

- Riyad Bank final letter of guarantee
- ANB performance bond
- SABB letter of guarantee
- SNB original guarantee instrument on colored security paper
- Saudi Investment Bank guarantee

Operational meaning:

- this is the active legal source document
- downstream requests and replies should treat it as the primary instrument

### Amendment / Extension / Change Notice

Observed examples include:

- Riyad Bank extension notice (`تمديد خطاب الضمان`)
- BSF corporate amendment letter (`تعديل خطاب الضمان`)
- bank-issued letters that describe a change without replacing the original
  instrument

Operational meaning:

- these documents do not replace the original guarantee by default
- they must be linked as amendment evidence unless a new original instrument is
  clearly issued

## Immediate Catalog Implications

The current document-family catalog should now treat these families as the
first real baseline:

- `riyad`
- `alinma`
- `bnp-paribas`
- `anb`
- `bsf`
- `sabb`
- `snb`
- `saib`

And the catalog should distinguish at least these structural classes:

- `original-instrument`
- `amendment-letter`
- `extension-notice`

## Business Rule Alignment

These samples support the agreed business rule:

- extension notices and separate amendment letters do **not** supersede the
  original instrument
- when the bank issues a fresh original guarantee document for the same
  guarantee after reduction, the previous original becomes `Superseded`

## Next Use

This sample set should be used for:

1. expanding the document-form catalog
2. OCR-provider contract design
3. intake field expectation rules
4. operations validation against real scanned material

## Canonical OCR Stack Direction

Because these baseline samples are scanned image PDFs without a usable text
layer, the OCR design for BG should now assume the following production path:

- `PyMuPDF` for text-layer probing and primary rasterization
- `PDFium` only when specific PDF compatibility issues require a fallback
- `OpenCV` for preprocessing and cleanup
- `LayoutParser` for layout and text-region detection
- `PaddleOCR` for recognition
- bank-form-aware post-processing into structured candidates

Important:

- `LayoutParser` should be treated as a mandatory stage for scanned or mixed
  documents
- this sample set is only the first baseline; the stack must remain extensible
  as more bank families and layouts are added
