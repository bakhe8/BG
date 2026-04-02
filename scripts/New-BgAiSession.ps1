param(
    [ValidateSet("Wave1", "Wave2", "Wave3", "Wave4")]
    [string]$Wave = "Wave1",

    [ValidateSet("A", "B", "C", "D", "E", "F", "G")]
    [string]$Axis,

    [string[]]$Models = @("gemini", "gpt4", "claude"),

    [string[]]$Surfaces = @(),

    [string[]]$Focus = @(),

    [string]$TaskContext,

    [string]$OutputRoot = ".ai\\sessions"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$promptScript = Join-Path $scriptRoot "Get-BgAiPrompt.ps1"

if (-not (Test-Path $promptScript)) {
    throw "Prompt generator not found: $promptScript"
}

$axisLabels = @{
    A = "Architecture & Boundaries"
    B = "Operational Model"
    C = "UI / UX"
    D = "Domain & Data Models"
    E = "Enterprise Closure Topics"
    F = "OCR & Document Intake"
    G = "Production Readiness"
}

$defaultWaveFocus = @{
    Wave1 = @(
        "current reality only",
        "evidence",
        "role/state fit",
        "immediate friction",
        "do not jump to broad redesign"
    )
    Wave2 = @(
        "gaps and failures",
        "misalignments",
        "what looks correct but is not enterprise-grade"
    )
    Wave3 = @(
        "target state",
        "blueprints",
        "migration path"
    )
    Wave4 = @(
        "sequencing",
        "cross-verification",
        "write-back targets",
        "program closure implications"
    )
}

function New-Slug {
    param([string]$Value)

    $slug = $Value.ToLowerInvariant()
    $slug = [regex]::Replace($slug, "[^a-z0-9]+", "-")
    return $slug.Trim("-")
}

function Get-RelativeRepoPath {
    param(
        [string]$RepoRootPath,
        [string]$TargetPath
    )

    $repoUri = New-Object System.Uri(($RepoRootPath.TrimEnd('\') + '\'))
    $targetUri = New-Object System.Uri($TargetPath)
    $relativeUri = $repoUri.MakeRelativeUri($targetUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('/', '\')
}

function Get-ModelTaskContext {
    param(
        [string]$ModelName,
        [string]$WaveName,
        [string]$AxisName,
        [string[]]$SurfaceNames,
        [string[]]$FocusPoints,
        [string]$ExtraTaskContext,
        [string]$SessionFolder,
        [string]$ResponseTarget
    )

    $lines = @()
    $lines += "$WaveName only."
    $lines += ""
    $lines += "Axis:"
    $lines += "- Axis ${AxisName}: $($axisLabels[$AxisName])"

    if ($SurfaceNames.Count -gt 0) {
        $lines += ""
        $lines += "Surfaces / Scope:"
        foreach ($surface in $SurfaceNames) {
            $lines += "- $surface"
        }
    }

    $lines += ""
    $lines += "Session rules:"
    $lines += "- stay on one axis only"
    $lines += "- use current reality first unless the wave explicitly says otherwise"
    $lines += "- use live docs write-back targets under docs/"
    $lines += "- do not use PROGRAM_REGISTRY.md as the write-back target"
    $lines += "- do not treat the wave as complete until the result is persisted into: $ResponseTarget"
    $lines += "- if direct file writing is unavailable, emit the full final markdown intended for: $ResponseTarget"
    $lines += "- do not stop at a short chat summary"

    if ($FocusPoints.Count -gt 0) {
        $lines += ""
        $lines += "Focus:"
        foreach ($focusPoint in $FocusPoints) {
            $lines += "- $focusPoint"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ExtraTaskContext)) {
        $lines += ""
        $lines += "Extra context:"
        $lines += $ExtraTaskContext.Trim()
    }

    switch ($ModelName) {
        "gemini" {
            $lines += ""
            $lines += "Cross-verification expectation:"
            $lines += "- after major UI/operator gaps, hand them to gpt4 for model/architecture cause review"
            $lines += "- hand them to claude for risk/go-live review"
        }
        "gpt4" {
            $lines += ""
            $lines += "Cross-verification expectation:"
            $lines += "- after major architecture/domain gaps, hand them to gemini for operator impact review"
            $lines += "- hand them to claude for readiness/risk review"
        }
        "claude" {
            $lines += ""
            $lines += "Cross-verification expectation:"
            $lines += "- after major readiness/risk gaps, hand them to gpt4 for architecture cause review"
            $lines += "- hand them to gemini for operator impact review"
        }
    }

    return ($lines -join [Environment]::NewLine)
}

$sessionNameParts = @(
    (Get-Date -Format "yyyyMMdd-HHmmss"),
    "wave-$($Wave.ToLowerInvariant())",
    "axis-$($Axis.ToLowerInvariant())"
)

if ($Surfaces.Count -gt 0) {
    $sessionNameParts += (New-Slug ($Surfaces -join "-"))
}

$sessionFolderName = $sessionNameParts -join "-"
$sessionRoot = Join-Path $repoRoot $OutputRoot
$sessionPath = Join-Path $sessionRoot $sessionFolderName

New-Item -ItemType Directory -Path $sessionPath -Force | Out-Null

$focusPoints = @()
$focusPoints += $defaultWaveFocus[$Wave]
$focusPoints += $Focus
$focusPoints = $focusPoints | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

$summaryLines = @()
$summaryLines += "# BG AI Session Pack"
$summaryLines += ""
$summaryLines += "- Wave: $Wave"
$summaryLines += "- Axis: `Axis $Axis - $($axisLabels[$Axis])"
$summaryLines += "- Models: $($Models -join ", ")"

if ($Surfaces.Count -gt 0) {
    $summaryLines += "- Scope: $($Surfaces -join "; ")"
}

$summaryLines += ""
$summaryLines += "## Result Persistence"
$summaryLines += '- A wave is not complete until the active model result is saved into its matching `response-*.md` file.'
$summaryLines += "- If the model cannot write files directly, it must emit the full final markdown intended for that file."
$summaryLines += "- Short chat summaries are not accepted as the completion artifact."
$summaryLines += ""
$summaryLines += "## Execution Order"
$summaryLines += "1. Start with the primary model for this axis."
$summaryLines += "2. Run one-axis review first."
$summaryLines += "3. Capture major gaps with stable IDs."
$summaryLines += "4. Cross-verify major gaps with another model."
$summaryLines += "5. Wait for Codex review."
$summaryLines += "6. Use explicit execution approval before any edit."
$summaryLines += "7. After execution, return to Codex for post-execution review."
$summaryLines += "8. Only then write accepted findings back to live docs under `docs/`."
$summaryLines += ""
$summaryLines += "## Suggested Primary Model"

$primaryModel = switch ($Axis) {
    "A" { "gpt4" }
    "B" { "gemini" }
    "C" { "gemini" }
    "D" { "gpt4" }
    "E" { "claude" }
    "F" { "gemini" }
    "G" { "claude" }
}

$summaryLines += "- $primaryModel"
$summaryLines += ""
$summaryLines += "## Suggested Live Docs Write-Back Targets"
$summaryLines += "- docs/instructions/AUDIT_REMEDIATION_BACKLOG.md"
$summaryLines += "- docs/frontend_reconstruction_plan.md"
$summaryLines += "- docs/ocr_implementation_plan.md"
$summaryLines += "- docs/instructions/PRODUCTION_RUNBOOK.md"
$summaryLines += "- docs/program_closure_backlog.md"
$summaryLines += ""
$summaryLines += "## Execution Gate"
$summaryLines += "- Default mode: audit only"
$summaryLines += "- Stop after each wave"
$summaryLines += "- Wait for Codex review"
$summaryLines += "- Use explicit execution approval before any edit"
$summaryLines += "- After execution, wait for Codex post-execution review"

Set-Content -Path (Join-Path $sessionPath "00-session-plan.md") -Value ($summaryLines -join [Environment]::NewLine) -Encoding UTF8

$gateLines = @()
$gateLines += "# Execution Gate Template"
$gateLines += ""
$gateLines += "Use this only after Codex review."
$gateLines += ""
$gateLines += '```text'
$gateLines += "CODEX EXECUTION GATE: APPROVED"
$gateLines += "Model: <gemini|gpt4|claude>"
$gateLines += "Session: $sessionFolderName"
$gateLines += "Scope: <approved implementation scope>"
$gateLines += "Allowed Files:"
$gateLines += "- <file 1>"
$gateLines += "- <file 2>"
$gateLines += "Target Outcome:"
$gateLines += "- <what must change>"
$gateLines += "Stop Conditions:"
$gateLines += "- <when to stop and return without continuing>"
$gateLines += "Post-Execution Review:"
$gateLines += "- After implementation, stop and return to Codex for review."
$gateLines += "- Do not treat execution as complete until Codex review says approved."
$gateLines += '```'

Set-Content -Path (Join-Path $sessionPath "01-execution-gate-template.md") -Value ($gateLines -join [Environment]::NewLine) -Encoding UTF8

foreach ($model in $Models) {
    $responseFileFullPath = Join-Path $sessionPath ("response-{0}.md" -f $model)
    $responseFileRelative = Get-RelativeRepoPath -RepoRootPath $repoRoot -TargetPath $responseFileFullPath
    $modelTaskContext = Get-ModelTaskContext -ModelName $model -WaveName $Wave -AxisName $Axis -SurfaceNames $Surfaces -FocusPoints $focusPoints -ExtraTaskContext $TaskContext -SessionFolder $sessionFolderName -ResponseTarget $responseFileRelative
    $promptOutput = & powershell -ExecutionPolicy Bypass -File $promptScript -Model $model -TaskContext $modelTaskContext

    $promptPath = Join-Path $sessionPath ("prompt-{0}.md" -f $model)
    Set-Content -Path $promptPath -Value $promptOutput -Encoding UTF8

    $responseTemplate = @()
    $responseTemplate += "# Response Intake - $model"
    $responseTemplate += ""
    $responseTemplate += "- Session: $sessionFolderName"
    $responseTemplate += "- Wave: $Wave"
    $responseTemplate += "- Axis: $Axis"
    $responseTemplate += "- Mode: Audit only until Codex approval"
    $responseTemplate += "- Result file: $responseFileRelative"
    $responseTemplate += ""
    $responseTemplate += "## Session Completion Rule"
    $responseTemplate += "- This wave is not complete until the full final result is saved in this file."
    $responseTemplate += "- If the model could not write files directly, paste the full final markdown here before moving on."
    $responseTemplate += "- Do not treat a short chat summary as completion."
    $responseTemplate += ""
    $responseTemplate += "## Accepted Major Gaps"
    $responseTemplate += "- Add accepted gap IDs here"
    $responseTemplate += ""
    $responseTemplate += "## Cross-Verification Targets"
    $responseTemplate += "- List which gaps should go to another model"
    $responseTemplate += ""
    $responseTemplate += "## Live Docs Write-Back Targets"
    $responseTemplate += "- Name the docs/ files that should receive accepted findings"
    $responseTemplate += ""
    $responseTemplate += "## Post-Execution Review Notes"
    $responseTemplate += "- Codex verdict:"
    $responseTemplate += "- Required corrections:"
    $responseTemplate += "- Codex takeover needed:"

    Set-Content -Path (Join-Path $sessionPath ("response-{0}.md" -f $model)) -Value ($responseTemplate -join [Environment]::NewLine) -Encoding UTF8
}

$readmeLines = @()
$readmeLines += "# How To Use This Session Pack"
$readmeLines += ""
$readmeLines += "1. Open the prompt file for the primary model first."
$readmeLines += "2. Paste it into the target model chat."
$readmeLines += "3. Save the full final result into the matching response file."
$readmeLines += "4. Send major gaps to the next model for cross-verification."
$readmeLines += "5. Wait for Codex review."
$readmeLines += "6. If execution is approved, use the execution gate template."
$readmeLines += "7. After execution, return to Codex for post-execution review."
$readmeLines += "8. Write accepted outcomes back into live docs under docs/ only after review acceptance."
$readmeLines += ""
$readmeLines += "A wave is not complete until the matching response file contains the full final result."

Set-Content -Path (Join-Path $sessionPath "README.md") -Value ($readmeLines -join [Environment]::NewLine) -Encoding UTF8

Write-Output "Created AI session pack:"
Write-Output $sessionPath
Write-Output ""
Write-Output "Primary model: $primaryModel"
Write-Output "Prompt files:"
Get-ChildItem -Path $sessionPath -Filter "prompt-*.md" | ForEach-Object {
    Write-Output ("- " + $_.FullName)
}
