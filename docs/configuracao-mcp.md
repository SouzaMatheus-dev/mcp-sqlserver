# Configuração MCP

O servidor MCP usa **stdio** (entrada/saída padrão). A configuração varia por cliente,
mas o padrão é sempre: `command` + `env` com `MSSQL_SERVER`.

---

## Gemini CLI

Arquivo: `%USERPROFILE%\.gemini\settings.json`

### Um ambiente (HML)

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true",
        "MSSQL_APPLICATION_INTENT_READONLY": "true",
        "MSSQL_MAX_ROWS": "200"
      }
    }
  }
}
```

### Vários ambientes (DEV + HML + PROD)

Veja o exemplo completo em [`examples/gemini-multi-ambiente.json`](examples/gemini-multi-ambiente.json).

```json
{
  "mcpServers": {
    "sqlserver-dev": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\DEV",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    },
    "sqlserver-hml": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    },
    "sqlserver-prod": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\PROD",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true",
        "MSSQL_MAX_ROWS": "100"
      }
    }
  }
}
```

Reinicie o Gemini CLI e confirme com `/mcp`.

---

## Cursor

Arquivo: `%USERPROFILE%\.cursor\mcp.json` (ou configuração MCP do projeto)

### NuGet (comando global)

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

### Python (venv local)

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "C:\\caminho\\mcp-sqlserver\\.venv\\Scripts\\mcp-sqlserver.exe",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

Alternativa com módulo Python:

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "C:\\caminho\\mcp-sqlserver\\.venv\\Scripts\\python.exe",
      "args": ["-m", "mcp_sqlserver"],
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master"
      }
    }
  }
}
```

Exemplo pronto: [`examples/cursor-sqlserver-hml.json`](examples/cursor-sqlserver-hml.json)

---

## Claude Desktop

Arquivo: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

Exemplo pronto: [`examples/claude-desktop-hml.json`](examples/claude-desktop-hml.json)

---

## VS Code (GitHub Copilot / MCP)

Arquivo: `.vscode/mcp.json` no workspace ou configuração global do VS Code.

```json
{
  "servers": {
    "sqlserver-hml": {
      "type": "stdio",
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

---

## Formato do MSSQL_SERVER

| Formato | Exemplo |
|---|---|
| Instância nomeada | `SRVSQL01\HML` |
| Instância padrão | `SRVSQL01` |
| Com porta | `SRVSQL01,1433` |
| Always On listener | `ag-listener.empresa.local` |

No JSON, escape a barra invertida: `"SRVSQL01\\HML"`.

---

## Um servidor, N bancos — quantas configs?

| Cenário | Configs no settings.json |
|---|---|
| HML com 10 bases no mesmo servidor | **1** |
| DEV + HML + PROD | **3** (uma por ambiente) |
| 10 bases em 10 servidores diferentes | **10** |

Use `MSSQL_DATABASE=master` como padrão e passe `database="NomeDoBanco"` nas ferramentas.

---

## Troubleshooting

| Problema | Solução |
|---|---|
| `MSSQL_SERVER não definida` | Verifique o bloco `env` no JSON |
| Login failed | Confirme que seu usuário de rede tem acesso ao SQL Server |
| Driver not found (Python) | Instale ODBC Driver 17/18 ou ajuste `MSSQL_DRIVER` |
| Comando não encontrado | Reinstale: `dotnet tool install --global McpSqlServer` |
| Gemini não lista MCP | Reinicie o CLI após editar `settings.json` |
