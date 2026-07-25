# McpSqlServer (.NET)

Pacote NuGet publicável em [nuget.org/profiles/matneves](https://www.nuget.org/profiles/matneves).

## Build e publish

```powershell
cd dotnet/McpSqlServer
dotnet pack -c Release -o ../nupkg

# Obtenha a API Key em https://www.nuget.org/account/apikeys
$env:NUGET_API_KEY = "sua-api-key-aqui"
dotnet nuget push ../nupkg/McpSqlServer.0.4.0.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY `
  --skip-duplicate
```

## Testar localmente (antes de publicar)

```powershell
dotnet pack -c Release -o ../nupkg

$env:MCPMSSQL_CONNECTION_STRING = "Server=SQLHML;Integrated Security=SSPI;TrustServerCertificate=True;Database=master"
dotnet dnx McpSqlServer --yes --source "C:\caminho\mcp-sqlserver\dotnet\nupkg"
```

## Uso corporativo (Gemini settings.json)

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "dotnet",
      "args": [
        "dnx",
        "McpSqlServer",
        "--yes",
        "--source",
        "https://api.nuget.org/v3/index.json"
      ],
      "env": {
        "MCPMSSQL_CONNECTION_STRING": "Server=SQLHML;Integrated Security=SSPI;TrustServerCertificate=True;Database=master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

Veja também: [docs/instalacao.md](../docs/instalacao.md)
