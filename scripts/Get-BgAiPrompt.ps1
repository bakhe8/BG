param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("gpt4", "gemini", "claude")]
    [string]$Model,

    [string]$TaskContext
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$docsMapPath = Join-Path $repoRoot "docs\README.md"
$governancePath = Join-Path $repoRoot ".ai\prompts\DOCS_GOVERNANCE.md"
$registryPath = Join-Path $repoRoot ".ai\prompts\PROGRAM_REGISTRY.md"
$qualityPath = Join-Path $repoRoot ".ai\prompts\QUALITY_CONTROL.md"
$enterpriseBriefPath = Join-Path $repoRoot ".ai\prompts\ENTERPRISE_REEVALUATION_BRIEF.md"
$modelPromptPath = Join-Path $repoRoot ".ai\prompts\$Model.md"

if (-not (Test-Path $modelPromptPath)) {
    throw "Model prompt not found: $modelPromptPath"
}

$sections = @()
$sections += "# BG External AI Runtime Prompt"
$sections += ""
$sections += "Repository root: $repoRoot"
$sections += ""
$sections += "## Required Startup"
$sections += "- Read the code first when possible."
$sections += "- Treat docs/README.md as the documentation map."
$sections += "- Treat docs/instructions/* as live instructions."
$sections += "- Treat .ai as a runtime overlay only."

if (-not [string]::IsNullOrWhiteSpace($TaskContext)) {
    $sections += ""
    $sections += "## Task Context"
    $sections += $TaskContext.Trim()
}

$sections += ""
$sections += "## Documentation Map"
$sections += (Get-Content $docsMapPath -Raw)
$sections += ""
$sections += "## AI Governance"
$sections += (Get-Content $governancePath -Raw)
$sections += ""
$sections += "## AI Runtime Registry"
$sections += (Get-Content $registryPath -Raw)
$sections += ""
$sections += "## Enterprise Re-Evaluation Brief"
$sections += (Get-Content $enterpriseBriefPath -Raw)
$sections += ""
$sections += "## AI Quality Control"
$sections += (Get-Content $qualityPath -Raw)
$sections += ""
$sections += "## Model Specialization"
$sections += (Get-Content $modelPromptPath -Raw)
$sections += ""
$sections += "## Non-Negotiable Output Contract"
$sections += "- Use the full 9-point contract from the Enterprise Re-Evaluation Brief; do not collapse it into a shorter overlay format."
$sections += "- Use dual prompting: load the Enterprise Re-Evaluation Brief first, then the model specialization overlay."
$sections += "- Start a fresh session with one axis only and focus on current reality before broad redesign."
$sections += "- Start with faceted scanning for the active axis; do not scan the whole repository broadly by default."
$sections += "- For operator-facing work, simulate the closest real role under realistic pressure, not a detached external observer."
$sections += "- Stay in audit mode by default and stop after each wave for Codex review."
$sections += "- Do not edit code or live docs unless the message explicitly contains: CODEX EXECUTION GATE: APPROVED."
$sections += "- After execution, stop again for Codex post-execution review before considering the task complete."
$sections += "- Respect the review outcomes: CODEX EXECUTION REVIEW: APPROVED / CHANGES REQUIRED / CODEX TAKEOVER."
$sections += "- No major gap is complete without a Mermaid blueprint."
$sections += "- Every Critical or High point must include the appropriate blueprint type."
$sections += "- Every structural or enterprise fix must include a Migration Path Map."
$sections += "- Assign stable gap IDs and cross-link related gaps, dependencies, and blockers."
$sections += "- Name the live docs/ write-back target for every major accepted finding."
$sections += "- A wave is not complete until the full result is persisted into the matching .ai/sessions/.../response-<model>.md file."
$sections += "- If direct file writing is unavailable, emit the full final markdown intended for that response file, clearly labeled as ready to save."
$sections += "- Do not rely on transient chat summaries as the completion artifact for a wave."
$sections += "- After a major gap is captured, route it to another model specialization for cross-verification when useful."
$sections += "- Do not stop at critique, prose target state, or backlog wording only."
$sections += "- Convert major findings into target state + blueprint + staged path."

$sections -join [Environment]::NewLine
