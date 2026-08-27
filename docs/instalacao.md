# Instalação

## Requisitos

- **Windows** com usuário de rede autorizado no SQL Server
- **ODBC Driver 17 ou 18 for SQL Server** — [download Microsoft](https://learn.microsoft.com/sql/connect/odbc/download-odbc-driver-for-sql-server)
- Acesso ao SQL Server com autenticação integrada (Windows)

---

## .NET: qual opção usar?

| Opção | SDK necessário | Instalação | Quando usar |
|---|---|---|---|
| **`dotnet dnx`** | **.NET 10** (10.0.100+) | Nenhuma — baixa do NuGet a cada execução | Máquina com SDK 10, zero setup |
| **Global tool** | **.NET 8+** | `dotnet tool install --global McpSqlServer` | Corporativo sem SDK 10 |
| **`dotnet tool exec`** | **.NET 10** | Alternativa ao `dnx` | Mesmo que dnx |

Verifique sua versão:

```powershell
dotnet --version
```

- `10.0.x` → pode usar **`dotnet dnx`** (Opção 1)
- `8.x` ou `9.x` → use **global tool** (Opção 2)

> O pacote **McpSqlServer** roda em .NET 8+, mas o comando **`dnx` só existe no SDK 10**.

---

## Opção 1 — NuGet com dotnet dnx (requer .NET 10 SDK)

Baixa e executa direto do NuGet, sem instalar manualmente. **Requer SDK 10.0.100 ou superior.**

### settings.json (Gemini / Cursor)

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
        "MSSQL_READONLY": "true",
        "MSSQL_MAX_ROWS": "200"
      }
    }
  }
}
```

Exemplo pronto: [`examples/gemini-nuget-dnx.json`](examples/gemini-nuget-dnx.json)

---

## Opção 2 — NuGet global tool (requer .NET 8+ SDK) — recomendado sem SDK 10

Funciona em **qualquer máquina com .NET 8 SDK ou superior**, inclusive corporativo sem .NET 10.

### Instalar

```powershell
dotnet tool install --global McpSqlServer
```

### Atualizar para nova versão

```powershell
dotnet tool update --global McpSqlServer
```

### settings.json

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "mcp-sqlserver",
      "env": {
        "MCPMSSQL_CONNECTION_STRING": "Server=SQLHML;Integrated Security=SSPI;TrustServerCertificate=True;Database=master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

Exemplo pronto: [`examples/gemini-global-tool.json`](examples/gemini-global-tool.json)

### Verificar

```powershell
dotnet tool list --global
mcp-sqlserver --version
where.exe mcp-sqlserver
```

### Desinstalar

```powershell
dotnet tool uninstall --global McpSqlServer
```

---

## Variáveis de ambiente (.NET)

| Variável | Descrição |
|---|---|
| `MCPMSSQL_CONNECTION_STRING` | Connection string completa (**recomendado**) |
| `MSSQL_SERVER` | Alternativa: servidor/instância |
| `MSSQL_DATABASE` | Banco padrão (default: `master`) |
| `MSSQL_READONLY` | `true` por padrão |
| `MSSQL_MAX_ROWS` | Limite de linhas (default: `200`) |

---

## Desenvolvimento (clone do repositório)

```powershell
git clone https://github.com/SouzaMatheus-dev/mcp-sqlserver.git
cd mcp-sqlserver
dotnet test dotnet/McpSqlServer.Tests/McpSqlServer.Tests.csproj -c Release
```

---

## Próximo passo

→ [Configuração MCP](configuracao-mcp.md)
