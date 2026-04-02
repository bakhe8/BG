$ErrorActionPreference = 'Stop'
Add-Type -Path "c:\Users\Bakheet\Documents\Projects\BG\src\BG.Web\bin\Debug\net8.0\Npgsql.dll"
$connString = "Host=127.0.0.1;Port=5432;Database=bg_app;Username=bg_app;Password=BgApp!2026#Local"
$conn = New-Object Npgsql.NpgsqlConnection($connString)
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = 'SELECT "Id", "Username", "DisplayName", "SourceType", "HasLocalPassword" FROM "Users"'
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Output ($reader["Username"].ToString() + " | " + $reader["DisplayName"].ToString() + " | " + $reader["HasLocalPassword"].ToString())
    }
} finally {
    $conn.Close()
}
