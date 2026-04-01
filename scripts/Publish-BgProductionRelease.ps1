param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "",
    [switch]$SkipToolRestore,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$ZipPackage
)

$ErrorActionPreference = "Stop"

function Get-GitValue {
    param(
        [string[]]$Arguments
    )

    try
    {
        $value = & git @Arguments 2>$null

        if ($LASTEXITCODE -eq 0)
        {
            return ($value | Select-Object -First 1)
        }
    }
    catch
    {
    }

    return ""
}

function New-ArtifactEntry {
    param(
        [string]$Path,
        [string]$Root
    )

    if (-not (Test-Path $Path))
    {
        return $null
    }

    $hash = Get-FileHash -Algorithm SHA256 -Path $Path
    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)

    if (-not $resolvedRoot.EndsWith([System.IO.Path]::DirectorySeparatorChar))
    {
        $resolvedRoot = $resolvedRoot + [System.IO.Path]::DirectorySeparatorChar
    }

    $relativeUri = New-Object System.Uri($resolvedRoot)
    $artifactUri = New-Object System.Uri($resolvedPath)
    $relativePath = [System.Uri]::UnescapeDataString($relativeUri.MakeRelativeUri($artifactUri).ToString()).Replace('/', '\')

    return [ordered]@{
        relativePath = $relativePath
        sizeBytes = (Get-Item $Path).Length
        sha256 = $hash.Hash.ToLowerInvariant()
    }
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $projectRoot "BG.sln"
$webProject = Join-Path $projectRoot "src\BG.Web\BG.Web.csproj"

if ([string]::IsNullOrWhiteSpace($OutputRoot))
{
    $OutputRoot = Join-Path $projectRoot ".artifacts\publish\BG.Web"
}

if (Test-Path $OutputRoot)
{
    Remove-Item $OutputRoot -Recurse -Force
}

if (-not $SkipToolRestore)
{
    Write-Host "Restoring local dotnet tools..." -ForegroundColor Cyan
    & dotnet tool restore

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet tool restore failed."
    }
}

if (-not $SkipBuild)
{
    Write-Host "Building solution ($Configuration)..." -ForegroundColor Cyan
    & dotnet build $solutionPath -c $Configuration -v minimal

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet build failed."
    }
}

if (-not $SkipTests)
{
    Write-Host "Running tests ($Configuration)..." -ForegroundColor Cyan

    $testArgs = @(
        "test", $solutionPath,
        "-c", $Configuration,
        "-v", "minimal"
    )

    if (-not $SkipBuild)
    {
        $testArgs += "--no-build"
    }

    & dotnet @testArgs

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet test failed."
    }
}

Write-Host "Publishing BG.Web ($Configuration)..." -ForegroundColor Cyan

$publishArgs = @(
    "publish", $webProject,
    "-c", $Configuration,
    "-o", $OutputRoot,
    "-v", "minimal"
)

if (-not $SkipBuild)
{
    $publishArgs += "--no-build"
}

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed."
}

$workerPath = Join-Path $OutputRoot "OcrWorker\ocr_worker.py"
$webConfigPath = Join-Path $OutputRoot "web.config"
$dllPath = Join-Path $OutputRoot "BG.Web.dll"
$manifestPath = Join-Path $OutputRoot "release-manifest.json"
$handoffRoot = Join-Path $OutputRoot "_handoff"

if (-not (Test-Path $dllPath))
{
    throw "Publish output is missing BG.Web.dll."
}

if (-not (Test-Path $webConfigPath))
{
    throw "Publish output is missing web.config."
}

if (-not (Test-Path $workerPath))
{
    throw "Publish output is missing OcrWorker\\ocr_worker.py."
}

Write-Host "Publish output ready: $OutputRoot" -ForegroundColor Green

$zipPath = ""

if ($ZipPackage)
{
    $zipPath = "$OutputRoot.zip"

    if (Test-Path $zipPath)
    {
        Remove-Item $zipPath -Force
    }

    Write-Host "Creating release archive..." -ForegroundColor Cyan
    Compress-Archive -Path (Join-Path $OutputRoot "*") -DestinationPath $zipPath -Force
    Write-Host "Release archive ready: $zipPath" -ForegroundColor Green
}

$manifest = [ordered]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    configuration = $Configuration
    publishOutputRoot = $OutputRoot
    git = [ordered]@{
        branch = (Get-GitValue -Arguments @("rev-parse", "--abbrev-ref", "HEAD"))
        commit = (Get-GitValue -Arguments @("rev-parse", "HEAD"))
    }
    execution = [ordered]@{
        toolRestoreSkipped = [bool]$SkipToolRestore
        buildSkipped = [bool]$SkipBuild
        testsSkipped = [bool]$SkipTests
        zipPackage = [bool]$ZipPackage
    }
    verification = [ordered]@{
        releaseBuildCommand = "dotnet build BG.sln -c $Configuration"
        releaseTestCommand = "dotnet test BG.sln -c $Configuration --no-build"
        productionEnvironmentCheck = ".\scripts\Test-BgProductionEnvironment.ps1 -SiteRoot 'C:\inetpub\BG'"
    }
    requiredArtifacts = @(
        (New-ArtifactEntry -Path $dllPath -Root $projectRoot),
        (New-ArtifactEntry -Path $webConfigPath -Root $projectRoot),
        (New-ArtifactEntry -Path $workerPath -Root $projectRoot)
    ) | Where-Object { $null -ne $_ }
}

if (-not [string]::IsNullOrWhiteSpace($zipPath) -and (Test-Path $zipPath))
{
    $manifest["releaseArchive"] = New-ArtifactEntry -Path $zipPath -Root $projectRoot
}

$manifestJson = $manifest | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText($manifestPath, $manifestJson, [System.Text.Encoding]::UTF8)

Write-Host "Release manifest ready: $manifestPath" -ForegroundColor Green

if (Test-Path $handoffRoot)
{
    Remove-Item $handoffRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $handoffRoot | Out-Null

$serverChecklistTemplate = Join-Path $projectRoot "docs\instructions\deployment\production_server_checklist.md"
$envTemplate = Join-Path $projectRoot "deploy\iis\web.config.environmentVariables.example.xml"
$releaseHandoffTemplate = Join-Path $projectRoot "docs\instructions\deployment\release_handoff_template.md"
$smokeRecordTemplate = Join-Path $projectRoot "docs\instructions\deployment\post_deploy_smoke_record.md"

Copy-Item $serverChecklistTemplate (Join-Path $handoffRoot "production_server_checklist.md")
Copy-Item $envTemplate (Join-Path $handoffRoot "web.config.environmentVariables.example.xml")
Copy-Item $releaseHandoffTemplate (Join-Path $handoffRoot "release_handoff_template.md")
Copy-Item $smokeRecordTemplate (Join-Path $handoffRoot "post_deploy_smoke_record.md")

$dllArtifact = $manifest.requiredArtifacts | Where-Object { $_.relativePath -like "*BG.Web.dll" } | Select-Object -First 1
$webConfigArtifact = $manifest.requiredArtifacts | Where-Object { $_.relativePath -like "*web.config" } | Select-Object -First 1
$workerArtifact = $manifest.requiredArtifacts | Where-Object { $_.relativePath -like "*ocr_worker.py" } | Select-Object -First 1
$archiveArtifact = $manifest.releaseArchive
$verification = $manifest["verification"]
$releaseBuildCommand = $verification["releaseBuildCommand"]
$releaseTestCommand = $verification["releaseTestCommand"]
$productionEnvironmentCheckCommand = ".\scripts\Test-BgProductionEnvironment.ps1 -SiteRoot C:\inetpub\BG"

$releaseHandoffPath = Join-Path $handoffRoot "release_handoff.md"
$releaseHandoff = @"
# Production Release Handoff

## Release Identity

- Environment: Production
- Release date: $($manifest.generatedAtUtc)
- Deployment window:
- Deployment operator:
- Database operator:
- Application owner:

## Build Provenance

- Branch: $($manifest.git.branch)
- Commit: $($manifest.git.commit)
- Publish folder: $OutputRoot
- Release archive: $zipPath
- Release manifest: $manifestPath

## Package Integrity

- `BG.Web.dll` SHA256: $($dllArtifact.sha256)
- `web.config` SHA256: $($webConfigArtifact.sha256)
- `OcrWorker\ocr_worker.py` SHA256: $($workerArtifact.sha256)
- Release archive SHA256: $($archiveArtifact.sha256)

## Required Server Inputs

- Public host name:
- TLS certificate thumbprint:
- `ConnectionStrings__PostgreSql` supplied:
- `Storage__DocumentsRoot` supplied:
- `DataProtection__KeysPath` supplied:
- `AllowedHosts` supplied:
- `OperationalSeed__Enabled=false` confirmed:
- `Swagger__Enabled=false` confirmed:
- `Ocr__Enabled` approved for this environment:

## Migration Plan

- Migration mode:
  - manual `dotnet-ef`
  - controlled startup migration
- Database backup completed:
- Rollback database backup location:

## Deployment Paths

- IIS site root: `C:\inetpub\BG`
- IIS backup root: `C:\inetpub\BG_backups`
- Documents root: `C:\BG\documents`
- Data-protection keys root: `C:\BG\data-protection`

## Validation Commands

- Release build: $releaseBuildCommand
- Release tests: $releaseTestCommand
- Server environment check: $productionEnvironmentCheckCommand
- Health check URL: https://<production-host>/health

## Handoff Status

- Package reviewed:
- Server checklist completed:
- Smoke sign-off completed:
- Ready for go-live:

## Notes

- Fill `production_server_checklist.md` before copying files to the server.
- Use `web.config.environmentVariables.example.xml` as the production values template.
- Fill `post_deploy_smoke_record.md` immediately after startup verification.
"@

[System.IO.File]::WriteAllText($releaseHandoffPath, $releaseHandoff, [System.Text.Encoding]::UTF8)

Write-Host "Release handoff package ready: $handoffRoot" -ForegroundColor Green
