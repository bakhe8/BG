# OCR Implementation Plan

## Document Role

- Status: `execution plan`
- Scope: live OCR implementation order, locked decisions, and completion criteria
- Documentation index: [README.md](README.md)
- Architecture baseline: [../ARCHITECTURE.md](../ARCHITECTURE.md)
- Real sample baseline: [reference/initial-bank-form-sample-catalog.md](reference/initial-bank-form-sample-catalog.md)

## Boundary

This file owns the live OCR implementation order.

- Use it for phase order, locked execution decisions, and completion criteria.
- Use [../ARCHITECTURE.md](../ARCHITECTURE.md) for the higher-level architectural baseline.
- Use [reference/initial-bank-form-sample-catalog.md](reference/initial-bank-form-sample-catalog.md) for the real-sample reference baseline.
- Do not let older audit or planning material override this file when OCR implementation details diverge.

## Purpose

This document defines the execution plan for replacing the current local
heuristic extraction path with a production OCR pipeline suitable for BG's real
scanned bank guarantee material.

It exists to prevent partial or ambiguous OCR work. From this point forward,
OCR implementation should follow this order unless a blocking technical issue
forces a documented deviation.

## Locked Decisions

The following decisions are already fixed and should not be revisited during
implementation:

- BG remains `text-first` when a usable PDF text layer exists.
- The scanned-document OCR path is:
  - `PyMuPDF`
  - `PDFium` fallback only where needed
  - `OpenCV`
  - `LayoutParser`
  - `PaddleOCR`
  - `post-processing`
  - `structured data`
  - `human review`
- `LayoutParser` is mandatory in the scanned or mixed-document path.
- The current real bank-form sample set is only a starting baseline and the
  implementation must remain extensible.
- The OCR pipeline must stay inside the hospital environment and must not
  depend on a cloud SaaS OCR provider.
- Large bank-document bundles must default to page-scoped OCR smoke and
  regression runs instead of full-document runs.

## Goals

1. Replace heuristic extraction with a provider-backed OCR pipeline.
2. Preserve the current BG review contract instead of breaking it.
3. Keep the integration replaceable and observable.
4. Make the pipeline extensible as more bank families and layouts arrive.

## Non-Goals

This phase should not:

- redesign the intake review UX again
- introduce ML-based autonomous approval or matching
- hardcode one bank layout into the core application layer
- tie BG directly to Python process details from Razor Pages

## Existing Contract To Preserve

The new OCR path must continue to produce the same BG-side review shape:

- scenario key
- document form key
- bank profile key
- field key
- value
- confidence
- provenance
- page number
- region or bounding box
- extraction route

This means OCR should replace the extraction engine internals, not the
downstream human review model.

## Execution Order

### Phase 1: Integration Boundary

Goal:
- Define a strict BG-side OCR integration contract.

Deliverables:
- `IOcrDocumentProcessingService` or equivalent BG-side boundary
- input contract for:
  - staged document path
  - scenario key
  - document form key
  - bank profile key
- output contract for:
  - structured field candidates
  - page metadata
  - region metadata
  - route and confidence metadata

Rules:
- the Application layer should depend on the contract, not the concrete OCR
  stack
- the existing `IIntakeExtractionEngine` remains the orchestration boundary on
  the BG side

Exit criteria:
- BG can swap between local heuristic extraction and OCR-backed extraction via
  dependency injection

### Phase 2: OCR Worker Skeleton

Goal:
- Stand up the concrete OCR processing worker or service boundary.

Deliverables:
- internal OCR worker project or integration host
- request/response serialization contract
- local execution strategy for:
  - `PyMuPDF`
  - `OpenCV`
  - `LayoutParser`
  - `PaddleOCR`
- error envelope model

Rules:
- keep Python-specific implementation details isolated from `BG.Application`
- do not let Razor Pages call Python scripts directly

Exit criteria:
- a staged PDF can be submitted to the worker and return a well-formed empty or
  populated extraction result

### Phase 3: PDF and Page Pipeline

Goal:
- Implement the physical page processing path.

Pipeline:
1. `PyMuPDF` text-layer probe
2. if usable text layer exists:
   - native text extraction path
3. else:
   - page rasterization via `PyMuPDF`
   - `PDFium` fallback only for compatibility failures
   - `OpenCV` preprocessing
   - `LayoutParser` detection
   - `PaddleOCR` recognition

Deliverables:
- page classification result
- rasterization service
- preprocessing module
- OCR route metadata
- page-selection support so large PDFs can be processed one page at a time

Exit criteria:
- scanned PDFs and text PDFs both produce route-aware processing results
- large sample bundles can be tested through selected-page requests without
  processing every page by default

### Phase 4: Structured Post-Processing

Goal:
- Turn OCR output into BG-native structured candidates.

Deliverables:
- field normalization rules
- Arabic/English mixed text cleanup
- date normalization
- amount normalization
- guarantee number normalization
- bank-form-aware mapping from OCR text regions to BG field keys

Rules:
- post-processing must read:
  - scenario definition
  - document form definition
  - bank profile definition
- do not normalize by raw regex alone when form-specific rules exist

Exit criteria:
- OCR output becomes `IntakeExtractionFieldCandidate` equivalents with stable
  provenance and confidence

### Phase 5: Human Review Preservation

Goal:
- Keep the operator review surface stable while improving its evidence quality.

Deliverables:
- preserve current review field rendering
- add page/region provenance where useful
- show extraction route clearly
- continue to require explicit human confirmation for critical fields

Rules:
- this phase improves evidence, not autonomy
- the operator remains the final authority before save

Exit criteria:
- the intake operator can review OCR-backed fields without learning a new flow

### Phase 6: Bank-Form Expansion

Goal:
- Expand from the first sample baseline to a maintainable bank-form library.

Deliverables:
- more bank profile entries
- more structural document classes
- per-bank expected field rules
- per-bank reference patterns

Rules:
- every new form should be added through catalog growth, not custom branching
- the form library must stay extensible for future banks and document variants

Exit criteria:
- new forms can be onboarded by extending profile and form catalogs with
  minimal impact on OCR pipeline internals

### Phase 7: Runtime Hardening

Goal:
- Make the OCR path operationally safe.

Deliverables:
- timeout policy
- structured logging
- failure classification
- retry policy where appropriate
- hosted and integration tests
- sample-based regression suite
- page-scoped sample manifests for large real-world bundles

Rules:
- OCR failure must not block the entire intake surface from loading
- partial extraction is allowed; silent corruption is not

Exit criteria:
- OCR failures degrade safely into reviewable states

## Recommended Delivery Sprints

### Sprint A

- Phase 1
- Phase 2

Outcome:
- the integration boundary exists and the worker can be called

### Sprint B

- Phase 3
- Phase 4

Outcome:
- the OCR path returns structured BG candidates from scanned PDFs

### Sprint C

- Phase 5
- Phase 6

Outcome:
- intake review is stable and bank-form coverage starts becoming useful

### Sprint D

- Phase 7

Outcome:
- OCR becomes safe for operational use on realistic seeded data

## Acceptance Criteria

The OCR implementation is not considered complete unless all of the following
are true:

- scanned PDFs from the real sample baseline produce structured candidates
- text-native PDFs still take the text-first path
- the intake operator can see route and provenance
- critical fields remain human-confirmed before save
- bank-form catalogs remain extensible
- failures are logged and recoverable
- automated tests cover both text-first and OCR-first paths

## Immediate Next Step

The next implementation step should be:

1. define the OCR integration contract
2. introduce the OCR worker boundary
3. keep the current heuristic extractor as a fallback while the worker path is
   being integrated

This is the safest path because it upgrades the extraction engine without
breaking the existing BG intake review model.
