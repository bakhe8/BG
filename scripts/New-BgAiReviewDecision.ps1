param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("APPROVED", "CHANGES_REQUIRED", "CODEX_TAKEOVER")]
    [string]$Decision,

    [Parameter(Mandatory = $true)]
    [ValidateSet("gemini", "gpt4", "claude")]
    [string]$Model,

    [string]$Session,

    [string[]]$Corrections = @(),

    [string[]]$AllowedFiles = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$lines = @()
$lines += "CODEX EXECUTION REVIEW: $Decision"
$lines += "Model: $Model"

if (-not [string]::IsNullOrWhiteSpace($Session)) {
    $lines += "Session: $Session"
}

switch ($Decision) {
    "APPROVED" {
        $lines += ""
        $lines += "Outcome:"
        $lines += "- The execution is accepted as completed."
        $lines += "- No further model edits are required for this scope."
        $lines += "- Ensure the final reviewed execution summary is persisted in the matching session response file."
    }
    "CHANGES_REQUIRED" {
        $lines += ""
        $lines += "Corrections:"
        if ($Corrections.Count -eq 0) {
            $lines += "- <list required corrections here>"
        }
        else {
            foreach ($correction in $Corrections) {
                $lines += "- $correction"
            }
        }

        $lines += ""
        $lines += "Allowed Files:"
        if ($AllowedFiles.Count -eq 0) {
            $lines += "- Reuse the original approved file scope unless Codex expands it explicitly."
        }
        else {
            foreach ($file in $AllowedFiles) {
                $lines += "- $file"
            }
        }

        $lines += ""
        $lines += "Rules:"
        $lines += "- Revise only inside the approved scope."
        $lines += "- Do not broaden the task."
        $lines += "- Update the matching session response file with the corrected final summary before stopping."
        $lines += "- After corrections, stop again for Codex review."
    }
    "CODEX_TAKEOVER" {
        $lines += ""
        $lines += "Outcome:"
        $lines += "- Stop editing immediately."
        $lines += "- Leave the remaining work to Codex."
        $lines += "- Do not continue implementation in this scope."
    }
}

$lines -join [Environment]::NewLine
