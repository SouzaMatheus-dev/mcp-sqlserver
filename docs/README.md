# Documentação — MCP-SQLServer

Índice dos guias do projeto.

| Guia | Descrição |
|---|---|
| [Instalação](instalacao.md) | NuGet, PyPI, requisitos e verificação |
| [Configuração MCP](configuracao-mcp.md) | Gemini, Cursor, Claude Desktop, VS Code |
| [Exemplos de uso](exemplos-uso.md) | Ferramentas, multi-banco, fluxos corporativos |
| [Publicação NuGet](publicacao-nuget.md) | Build, pack, publish e CI/CD |

## Exemplos JSON prontos

Copie e adapte os arquivos em [`examples/`](examples/):

| Arquivo | Uso |
|---|---|
| [`gemini-multi-ambiente.json`](examples/gemini-multi-ambiente.json) | DEV + HML + PROD no Gemini |
| [`gemini-python-venv.json`](examples/gemini-python-venv.json) | Gemini com venv Python local |
| [`cursor-sqlserver-hml.json`](examples/cursor-sqlserver-hml.json) | Cursor com NuGet global tool |
| [`claude-desktop-hml.json`](examples/claude-desktop-hml.json) | Claude Desktop com HML |

## Variáveis de ambiente (referência rápida)

```
MSSQL_SERVER=SRVSQL01\HML          # obrigatória
MSSQL_DATABASE=master              # banco padrão
MSSQL_READONLY=true                # somente leitura (padrão)
MSSQL_APPLICATION_INTENT_READONLY=true
MSSQL_MAX_ROWS=200
MSSQL_CONNECTION_TIMEOUT=15
MSSQL_DRIVER=ODBC Driver 17 for SQL Server   # apenas Python
```

## Instalação em uma linha

```powershell
# NuGet (recomendado)
dotnet tool install --global McpSqlServer

# PyPI
pip install mcp-sqlserver
```
