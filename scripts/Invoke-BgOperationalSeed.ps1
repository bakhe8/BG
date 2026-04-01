[CmdletBinding()]
param(
    [string]$Environment = "Development",
    [switch]$NoBuild
)

$projectPath = Join-Path $PSScriptRoot "..\src\BG.Web\BG.Web.csproj"
$projectPath = [System.IO.Path]::GetFullPath($projectPath)

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Unable to find BG.Web project at '$projectPath'."
}

$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT

try {
    $env:ASPNETCORE_ENVIRONMENT = $Environment

    $arguments = @(
        "run"
        "--project"
        $projectPath
    )

    if ($NoBuild) {
        $arguments += "--no-build"
    }

    $arguments += "--"
    $arguments += "--seed-operational-demo"

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    if ([string]::IsNullOrWhiteSpace($previousEnvironment)) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    }
}
