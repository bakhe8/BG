param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("gemini", "gpt4", "claude")]
    [string]$Model,

    [Parameter(Mandatory = $true)]
    [string]$Scope,

    [string]$Session,

    [string[]]$AllowedFiles = @(),

    [string[]]$TargetOutcome = @(),

    [string[]]$StopConditions = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$lines = @()
$lines += "CODEX EXECUTION GATE: APPROVED"
$lines += "Model: $Model"

if (-not [string]::IsNullOrWhiteSpace($Session)) {
    $lines += "Session: $Session"
}

$lines += "Scope: $Scope"
$lines += ""
$lines += "Allowed Files:"

if ($AllowedFiles.Count -eq 0) {
    $lines += "- <list allowed files here>"
}
else {
    foreach ($file in $AllowedFiles) {
        $lines += "- $file"
    }
}

$lines += ""
$lines += "Target Outcome:"

if ($TargetOutcome.Count -eq 0) {
    $lines += "- <state the intended result here>"
}
else {
    foreach ($item in $TargetOutcome) {
        $lines += "- $item"
    }
}

$lines += ""
$lines += "Stop Conditions:"

if ($StopConditions.Count -eq 0) {
    $lines += "- Stop if the required change touches files outside the approved scope."
    $lines += "- Stop if evidence in code conflicts with the approved gap statement."
    $lines += "- Stop if a live docs change is needed but not explicitly allowed."
}
else {
    foreach ($condition in $StopConditions) {
        $lines += "- $condition"
    }
}

$lines += ""
$lines += "Rules:"
$lines += "- Execute only within the approved scope."
$lines += "- Do not broaden the task on your own."
$lines += "- If the scope is insufficient, stop and return with the blocking reason."
$lines += "- After execution, report changed files, verification performed, and residual risks."
$lines += "- Persist the final execution summary into the matching session response file before treating the task as complete."

$lines -join [Environment]::NewLine
