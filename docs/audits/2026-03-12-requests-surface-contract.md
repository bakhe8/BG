# 2026-03-12 Requests Surface Contract

Purpose:
- إغلاق `UX-3`.
- جعل `Requests` سطح إنشاء ومتابعة مختصرة، لا قائمة ledger مطولة.

## Surface Split

### Creation Surface
Owns:
- request creation fields
- pre-submit workflow preview

Must explain:
- what route will be used after submission
- which fields are required for the selected request type

### Owned Request Tracking Surface
Owns:
- request summary
- current stage
- owner-facing state explanation
- last decision summary
- submit action when still actionable by owner

Must not dominate the card with:
- full timeline by default

## Ledger Contract

The ledger remains visible but secondary:
- available through expandable detail
- not equal in weight to the current state summary

## Acceptance Closure

The owner can now understand:
- current status
- current stage
- whether the request still needs submission
- what the latest decision means
without reading the full ledger first.
