# BG AI Prompt Pack

This folder contains model-specific prompt overlays for external AI sessions.

They are organized by specialization:

- [gpt4.md](gpt4.md)
  Architecture, boundaries, domain/data fitness
- [gemini.md](gemini.md)
  UI/UX, OCR behavior, operator-facing workflows
- [claude.md](claude.md)
  strategic closure, readiness, security, production risks

## Critical Rule

These prompts do **not** replace repo truth.

Every external model session must anchor itself in this order:

1. `/src`
2. [../../docs/README.md](../../docs/README.md)
3. live instruction files under `docs/instructions`
4. active execution plans under `/docs`
5. `.ai/prompts/*` as optional runtime guidance only

## Recommended Use

Generate a consolidated runtime prompt with:

```powershell
.\scripts\Get-BgAiPrompt.ps1 -Model gpt4
.\scripts\Get-BgAiPrompt.ps1 -Model gemini
.\scripts\Get-BgAiPrompt.ps1 -Model claude
```

This avoids manual copy-paste mistakes and keeps the model aligned with the
current repository structure.
