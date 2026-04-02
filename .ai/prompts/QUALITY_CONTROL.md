# BG AI Quality Control

Use this file to keep external model sessions disciplined.

## Minimum Output Quality

Every serious model output should include:

- current reality
- direct evidence from code or live docs
- target state
- concrete next step

If the model cannot provide evidence, its output is not ready to act on.

## Good Session Pattern

1. Read `docs/README.md`
2. Read the topic source document
3. Inspect the actual file(s) in `src/` or `tests/`
4. Produce findings or implementation steps
5. Write any accepted outcome back to the real docs

## Cross-Model Use

- `gemini`
  Use for operator surfaces, OCR usability, cognitive load, and review contracts.
- `gpt4`
  Use for architecture, data fitness, model boundaries, and structural critique.
- `claude`
  Use for production closure, risk, security, readiness, and strategic tradeoffs.

## Quality Checklist

- [ ] grounded in current code
- [ ] aligned with `docs/README.md`
- [ ] no invented source-of-truth hierarchy
- [ ] no placeholder backlog entries
- [ ] no broken local file links
