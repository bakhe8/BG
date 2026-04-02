# BG AI Documentation Governance

`.ai` is a runtime layer for external model sessions.

It is **not** an alternative documentation system.

## Hierarchy of Truth

When an external model is working on `BG`, it must follow this order:

1. `src/*`
   The code is the final truth.
2. [../../docs/README.md](../../docs/README.md)
   The documentation map and topic ownership.
3. `docs/instructions/*`
   Live operational, architecture, testing, and production instructions.
4. live execution plans under `/docs`
   For example:
   - `docs/ocr_implementation_plan.md`
   - `docs/frontend_reconstruction_plan.md`
   - `docs/refactor_roadmap.md`
5. `.ai/prompts/*`
   Session overlays only.

## Governance Rules

- If `.ai` conflicts with code or live docs, `.ai` is wrong.
- `.ai` may specialize model behavior, but it may not redefine repository truth.
- Any reusable insight discovered through an AI session must be written back to
  the real docs under `docs/` if it becomes operationally relevant.

## Practical Effect

Use `.ai` to:

- shape a model's perspective
- keep review format disciplined
- route the model toward the right domain focus

Do not use `.ai` to:

- invent a second backlog
- override `docs/instructions`
- replace operational source documents
