param(
    [string]$SiteRoot = "C:\inetpub\BG"
)

$ErrorActionPreference = "Stop"

function Resolve-DeployPath {
    param(
        [string]$ConfiguredPath,
        [string]$BasePath
    )

    if ([string]::IsNullOrWhiteSpace($ConfiguredPath))
    {
        return $ConfiguredPath
    }

    if ([System.IO.Path]::IsPathRooted($ConfiguredPath))
    {
        return $ConfiguredPath
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $ConfiguredPath))
}

$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

$environmentName = $env:ASPNETCORE_ENVIRONMENT
$connectionString = $env:ConnectionStrings__PostgreSql
$documentsRoot = $env:Storage__DocumentsRoot
$dataProtectionKeysPath = $env:DataProtection__KeysPath
$allowedHosts = $env:AllowedHosts
$swaggerEnabled = $env:Swagger__Enabled
$operationalSeedEnabled = $env:OperationalSeed__Enabled
$ocrEnabled = $env:Ocr__Enabled
$ocrPythonPath = $env:Ocr__PythonExecutablePath
$ocrWorkerPath = $env:Ocr__WorkerScriptPath
$ocrMaxFileSizeBytes = $env:Ocr__MaxFileSizeBytes
$ocrQueueCapacity = $env:Ocr__QueueCapacity
$knownProxies = Get-ChildItem Env: | Where-Object Name -like "ReverseProxy__KnownProxies__*"
$knownNetworks = Get-ChildItem Env: | Where-Object Name -like "ReverseProxy__KnownNetworks__*"

if ($environmentName -ne "Production")
{
    $failures.Add("ASPNETCORE_ENVIRONMENT must be set to Production.")
}

if ([string]::IsNullOrWhiteSpace($connectionString))
{
    $failures.Add("ConnectionStrings__PostgreSql is missing.")
}
elseif ($connectionString -match "(?i)Password\s*=\s*change-me")
{
    $failures.Add("ConnectionStrings__PostgreSql still uses the placeholder password change-me.")
}

if ([string]::IsNullOrWhiteSpace($allowedHosts))
{
    $failures.Add("AllowedHosts is missing.")
}
else
{
    $hosts = $allowedHosts.Split(@(';', ','), [System.StringSplitOptions]::RemoveEmptyEntries -bor [System.StringSplitOptions]::TrimEntries)

    if ($hosts.Count -eq 0 -or $hosts -contains "*")
    {
        $failures.Add("AllowedHosts must not be blank and cannot contain *.")
    }
}

if (($knownProxies | Measure-Object).Count -eq 0 -and ($knownNetworks | Measure-Object).Count -eq 0)
{
    $failures.Add("ReverseProxy__KnownProxies__* or ReverseProxy__KnownNetworks__* must be configured.")
}

if ([string]::IsNullOrWhiteSpace($documentsRoot))
{
    $failures.Add("Storage__DocumentsRoot is missing.")
}
else
{
    try
    {
        New-Item -ItemType Directory -Force -Path $documentsRoot | Out-Null
    }
    catch
    {
        $failures.Add("Storage__DocumentsRoot could not be prepared: $($_.Exception.Message)")
    }
}

if ([string]::IsNullOrWhiteSpace($dataProtectionKeysPath))
{
    $failures.Add("DataProtection__KeysPath is missing.")
}
else
{
    try
    {
        New-Item -ItemType Directory -Force -Path $dataProtectionKeysPath | Out-Null
    }
    catch
    {
        $failures.Add("DataProtection__KeysPath could not be prepared: $($_.Exception.Message)")
    }
}

if ($swaggerEnabled -and $swaggerEnabled.ToLowerInvariant() -eq "true")
{
    $failures.Add("Swagger__Enabled must remain false in production.")
}

if ($operationalSeedEnabled -and $operationalSeedEnabled.ToLowerInvariant() -eq "true")
{
    $failures.Add("OperationalSeed__Enabled must remain false in production.")
}

$siteDllPath = Join-Path $SiteRoot "BG.Web.dll"
$siteWebConfigPath = Join-Path $SiteRoot "web.config"

if (-not (Test-Path $siteDllPath))
{
    $failures.Add("SiteRoot does not contain BG.Web.dll: $siteDllPath")
}

if (-not (Test-Path $siteWebConfigPath))
{
    $failures.Add("SiteRoot does not contain web.config: $siteWebConfigPath")
}

try
{
    $runtimes = & dotnet --list-runtimes

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet --list-runtimes failed."
    }

    if (-not ($runtimes | Select-String -Pattern '^Microsoft\.AspNetCore\.App 8\.'))
    {
        $failures.Add("Microsoft.AspNetCore.App 8.x runtime is not installed.")
    }
}
catch
{
    $failures.Add("Unable to verify installed dotnet runtimes: $($_.Exception.Message)")
}

$ocrEnabledValue = if ([string]::IsNullOrWhiteSpace($ocrEnabled)) { "false" } else { $ocrEnabled.ToLowerInvariant() }

if ($ocrEnabledValue -eq "true")
{
    if ([string]::IsNullOrWhiteSpace($ocrPythonPath))
    {
        $failures.Add("Ocr__PythonExecutablePath is missing while OCR is enabled.")
    }
    elseif (-not (Test-Path $ocrPythonPath))
    {
        $failures.Add("Ocr__PythonExecutablePath does not exist: $ocrPythonPath")
    }

    if ([string]::IsNullOrWhiteSpace($ocrWorkerPath))
    {
        $failures.Add("Ocr__WorkerScriptPath is missing while OCR is enabled.")
    }
    else
    {
        $resolvedWorkerPath = Resolve-DeployPath -ConfiguredPath $ocrWorkerPath -BasePath $SiteRoot

        if (-not (Test-Path $resolvedWorkerPath))
        {
            $failures.Add("Ocr__WorkerScriptPath does not exist: $resolvedWorkerPath")
        }
    }

    $parsedOcrMaxFileSizeBytes = 0L
    if (-not [long]::TryParse($ocrMaxFileSizeBytes, [ref]$parsedOcrMaxFileSizeBytes) -or $parsedOcrMaxFileSizeBytes -le 0)
    {
        $failures.Add("Ocr__MaxFileSizeBytes must be greater than zero while OCR is enabled.")
    }

    $parsedOcrQueueCapacity = 0
    if (-not [int]::TryParse($ocrQueueCapacity, [ref]$parsedOcrQueueCapacity) -or $parsedOcrQueueCapacity -le 0)
    {
        $failures.Add("Ocr__QueueCapacity must be greater than zero while OCR is enabled.")
    }
}
else
{
    $warnings.Add("OCR is disabled for this environment.")
}

if ($failures.Count -gt 0)
{
    Write-Host "Production environment check failed:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" -ForegroundColor Red }

    if ($warnings.Count -gt 0)
    {
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        $warnings | ForEach-Object { Write-Host "- $_" -ForegroundColor Yellow }
    }

    throw "Production environment validation failed."
}

Write-Host "Production environment check passed." -ForegroundColor Green

if ($warnings.Count -gt 0)
{
    Write-Host ""
    Write-Host "Warnings:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "- $_" -ForegroundColor Yellow }
}
