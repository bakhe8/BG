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

$sections -join [Environment]::NewLine
