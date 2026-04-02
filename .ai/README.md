# BG AI Runtime Layer

This folder is the temporary external-model runtime layer for `BG`.

It exists to help you run structured sessions with models such as:

- `gpt4`
- `gemini`
- `claude`

It is **not** the source of truth for the project.
It exists only for the development and re-evaluation phase until `BG` reaches
the intended production closure level.

Authoritative order remains:

1. `/src`
2. [`docs/README.md`](../docs/README.md)
3. `docs/instructions/*`
4. active execution plans under `/docs`

Use `.ai` only to:

- bootstrap a focused model session
- force a consistent review format
- route a model toward the correct work area
- drive the multi-model enterprise re-evaluation program during development

Do not use `.ai` to override code or live documentation.

## Temporary Status

This folder is intentionally temporary developer tooling.

Once the project reaches the desired production closure point:

- remove `.ai/`
- remove `scripts/Get-BgAiPrompt.ps1`
- remove any documentation references that still point to `.ai`

The product must remain fully valid without this folder.

## How to use

Generate the runtime prompt for a model with:

```powershell
.\scripts\Get-BgAiPrompt.ps1 -Model gpt4
.\scripts\Get-BgAiPrompt.ps1 -Model gemini
.\scripts\Get-BgAiPrompt.ps1 -Model claude
```

Optional:

```powershell
.\scripts\Get-BgAiPrompt.ps1 -Model gemini -TaskContext "Review OCR quality on attachments.pdf"
```

The script prints one consolidated prompt that:

- anchors the model to the current repo truth
- points it to the correct live documents
- adds the selected model specialization
