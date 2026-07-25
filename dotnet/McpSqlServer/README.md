# McpSqlServer (.NET)

Servidor MCP para SQL Server com autenticação Windows e modo somente leitura.

## Variáveis de ambiente

| Variável | Descrição |
|---|---|
| `MCPMSSQL_CONNECTION_STRING` | Connection string completa (recomendado corporativo) |
| `MSSQL_SERVER` | Alternativa: servidor/instância |
| `MSSQL_DATABASE` | Banco padrão (default: `master`) |
| `MSSQL_READONLY` | `true` por padrão |
| `MSSQL_MAX_ROWS` | Limite de linhas (default: `200`) |

## Exemplo corporativo (Gemini settings.json)

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
