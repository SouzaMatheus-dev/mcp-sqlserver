# Configura o Gemini CLI para usar McpSqlServer via NuGet (dotnet dnx)
#
# Uso: .\scripts\setup-gemini.ps1
#      .\scripts\setup-gemini.ps1 -Server "SRVSQL01\HML"

param(
    [string]$Server = "SQLHML",
    [string]$Database = "master"
)

$ErrorActionPreference = "Stop"

$geminiDir = Join-Path $env:USERPROFILE ".gemini"
$settingsPath = Join-Path $geminiDir "settings.json"

if (-not (Test-Path $geminiDir)) {
    New-Item -ItemType Directory -Path $geminiDir -Force | Out-Null
}

$connectionString = "Server=$Server;Integrated Security=SSPI;TrustServerCertificate=True;Database=$Database"

$config = @{
    mcpServers = @{
        "sqlserver-hml" = @{
            command = "dotnet"
            args = @(
                "dnx",
                "McpSqlServer",
                "--yes",
                "--source",
                "https://api.nuget.org/v3/index.json"
            )
            env = @{
                MCPMSSQL_CONNECTION_STRING = $connectionString
                MSSQL_READONLY = "true"
                MSSQL_APPLICATION_INTENT_READONLY = "true"
                MSSQL_MAX_ROWS = "200"
            }
        }
    }
}

$config | ConvertTo-Json -Depth 10 | Set-Content -Path $settingsPath -Encoding UTF8

Write-Host "Configurado: $settingsPath" -ForegroundColor Green
Write-Host "Servidor: $Server" -ForegroundColor Cyan
Write-Host "Connection: $connectionString" -ForegroundColor Cyan
Write-Host ""
Write-Host "Proximo passo: reinicie o Gemini CLI e digite /mcp" -ForegroundColor Yellow
