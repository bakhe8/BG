param()

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$webProject = Join-Path $projectRoot "src\BG.Web"

$secretLines = & dotnet user-secrets list --project $webProject

if ($LASTEXITCODE -ne 0)
{
    throw "Unable to read user-secrets for BG.Web."
}

$secretMap = @{}

foreach ($line in $secretLines)
{
    if ($line -match "^(?<key>.+?) = (?<value>.*)$")
    {
        $secretMap[$Matches["key"]] = $Matches["value"]
    }
}

$connectionString = $secretMap["ConnectionStrings:PostgreSql"]

if ([string]::IsNullOrWhiteSpace($connectionString))
{
    throw "Missing ConnectionStrings:PostgreSql in user-secrets."
}

$connectionParts = @{}

foreach ($segment in ($connectionString -split ";"))
{
    if ([string]::IsNullOrWhiteSpace($segment))
    {
        continue
    }

    $parts = $segment -split "=", 2

    if ($parts.Count -eq 2)
    {
        $connectionParts[$parts[0].Trim()] = $parts[1].Trim()
    }
}

$documentsRoot = $secretMap["Storage:DocumentsRoot"]
$psqlPath = "C:\PostgreSQL\16\bin\psql.exe"

if (-not (Test-Path $psqlPath))
{
    $psqlPath = "psql"
}

$serverHost = if ($connectionParts.ContainsKey("Host")) { [string]$connectionParts["Host"] } else { "127.0.0.1" }
$port = if ($connectionParts.ContainsKey("Port")) { [string]$connectionParts["Port"] } else { "5432" }
$database = [string]$connectionParts["Database"]
$username = if ($connectionParts.ContainsKey("Username")) { [string]$connectionParts["Username"] } else { [string]$connectionParts["User ID"] }
$password = [string]$connectionParts["Password"]

if ([string]::IsNullOrWhiteSpace($password))
{
    throw "The PostgreSQL connection string does not contain a password."
}

$env:PGPASSWORD = $password

try
{
    Write-Host "Testing PostgreSQL connection for BG..." -ForegroundColor Cyan
    & $psqlPath -h $serverHost -p $port -U $username -d $database -w -c "select current_user, current_database();"

    if ($LASTEXITCODE -ne 0)
    {
        throw "PostgreSQL connection test failed."
    }

    if (-not [string]::IsNullOrWhiteSpace($documentsRoot))
    {
        if (Test-Path $documentsRoot)
        {
            Write-Host "Documents root: $documentsRoot" -ForegroundColor Green
        }
        else
        {
            Write-Warning "Documents root does not exist yet: $documentsRoot"
        }
    }

    Write-Host "BG local environment is ready." -ForegroundColor Green
}
finally
{
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
