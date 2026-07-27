# MCP-SQLServer

Servidor MCP corporativo para **SQL Server** com **autenticação integrada do Windows**
e **modo somente leitura** por padrão.

Conecte Gemini, Cursor, Claude Desktop ou VS Code ao SQL Server usando o **seu usuário
de rede** — sem credenciais de aplicação.

## Pacotes disponíveis

| Registro | Pacote | Instalação | SDK necessário |
|---|---|---|---|
| **NuGet** (.NET) | `McpSqlServer` | `dotnet tool install --global McpSqlServer` | .NET **8+** |
| **NuGet** (dnx) | `McpSqlServer` | `dotnet dnx McpSqlServer --yes` | .NET **10+** |
| **PyPI** (Python) | `mcp-sqlserver` | `pip install mcp-sqlserver` | Python 3.10+ |

> **`dotnet dnx` exige SDK 10.** Em máquinas corporativas com .NET 8/9, use a **global tool** (`mcp-sqlserver`).
> Veja [Instalação](docs/instalacao.md) para detalhes.

## Início rápido (NuGet — global tool, .NET 8+)

```powershell
dotnet tool install --global McpSqlServer
```

Configure no Gemini (`%USERPROFILE%\.gemini\settings.json`):

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

Reinicie o cliente MCP e valide com a ferramenta `usuario_conectado`.

## Início rápido (NuGet — dotnet dnx, requer .NET 10 SDK)

```powershell
dotnet --version   # precisa ser 10.0.x
```

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "dotnet",
      "args": ["dnx", "McpSqlServer", "--yes", "--source", "https://api.nuget.org/v3/index.json"],
      "env": {
        "MCPMSSQL_CONNECTION_STRING": "Server=SQLHML;Integrated Security=SSPI;TrustServerCertificate=True;Database=master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

## Início rápido (Python / PyPI)

```powershell
pip install mcp-sqlserver
```

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

## Um servidor, vários bancos

**Não** é necessário configurar um MCP por banco. Uma entrada por **servidor/ambiente**
(DEV, HML, PROD) basta — o parâmetro `database` nas ferramentas seleciona o banco:

```
listar_bancos()
listar_tabelas(database="Vendas")
executar_consulta(sql="SELECT TOP 10 * FROM dbo.Pedidos", database="Financeiro")
```

## Documentação

| Guia | Conteúdo |
|---|---|
| [Instalação](docs/instalacao.md) | NuGet, PyPI, requisitos e verificação |
| [Configuração MCP](docs/configuracao-mcp.md) | Gemini, Cursor, Claude Desktop, VS Code |
| [Exemplos de uso](docs/exemplos-uso.md) | Ferramentas, multi-banco, multi-ambiente |
| [Publicação NuGet](docs/publicacao-nuget.md) | Build, pack e publish do pacote .NET |

## Exemplos prontos

Arquivos JSON de referência em [`docs/examples/`](docs/examples/):

- `gemini-nuget-dnx.json` — `dotnet dnx` (**requer .NET 10 SDK**)
- `gemini-global-tool.json` — global tool (**requer .NET 8+ SDK**)
- `gemini-multi-ambiente.json` — DEV + HML + PROD
- `cursor-sqlserver-hml.json` — Cursor com servidor HML
- `claude-desktop-hml.json` — Claude Desktop

## Ferramentas MCP

| Ferramenta | Descrição |
|---|---|
| `usuario_conectado` | Login Windows e modo MCP |
| `listar_bancos` | Bancos visíveis no servidor |
| `listar_tabelas` | Tabelas base |
| `listar_views` | Views |
| `listar_procedures` | Procedures e functions (metadados) |
| `descrever_tabela` | Colunas de tabela |
| `descrever_view` | Colunas de view |
| `descrever_procedure` | Parâmetros de procedure |
| `obter_definicao_sql` | Script SQL do objeto |
| `consultar_view` | `SELECT TOP` seguro em view |
| `executar_consulta` | `SELECT`/`WITH` validado |
| `listar_chaves_estrangeiras` | Foreign keys (JOINs) |
| `buscar_coluna` | Busca colunas pelo nome |
| `buscar_objeto` | Busca tabelas/views/procedures |
| `obter_documentacao_objeto` | MS_Description (dicionário de dados) |
| `listar_indices` | Índices, unique, PK por tabela |
| `listar_dependencias` | Quem referencia / é referenciado por um objeto |
| `resumir_banco` | Contagens e TOP 20 maiores tabelas |
| `amostrar_tabela` | Amostra linhas de uma tabela (SELECT TOP) |
| `perfil_coluna` | Estatísticas de coluna (nulos, distintos, min/max) |
| `buscar_texto_sql` | Busca texto em views/procedures/functions |

> Procedures **não são executadas** (`EXEC` bloqueado). Foco em leitura corporativa.

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `MSSQL_SERVER` | — | Servidor/instância (**obrigatória**) |
| `MSSQL_DATABASE` | `master` | Banco padrão |
| `MSSQL_DRIVER` | `ODBC Driver 17 for SQL Server` | Driver ODBC (Python) |
| `MSSQL_READONLY` | `true` | Bloqueia DDL/DML/`EXEC` |
| `MSSQL_APPLICATION_INTENT_READONLY` | `true` | `ApplicationIntent=ReadOnly` |
| `MSSQL_MAX_ROWS` | `200` | Limite de linhas retornadas |
| `MSSQL_CONNECTION_TIMEOUT` | `15` | Timeout de conexão (segundos) |

## Segurança

- Autenticação via usuário de rede (`Trusted_Connection` / `Integrated Security`)
- Validador SQL bloqueia comandos de escrita
- Permissões reais vêm do SQL Server — o MCP não eleva privilégios
- Mantenha `MSSQL_READONLY=true` em produção

## Desenvolvimento

```powershell
git clone https://github.com/SouzaMatheus-dev/mcp-sqlserver.git
cd mcp-sqlserver
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
.\.venv\Scripts\python.exe -m pytest
```

## Licença

MIT — veja [LICENSE](LICENSE).
