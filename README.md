# MCP-SQLServer

Servidor MCP corporativo para **SQL Server** com **autenticação integrada do Windows**
(`Trusted_Connection`) e **modo somente leitura** por padrão.

O processo roda na sua máquina com a sua sessão de rede. O SQL Server enxerga a
conexão como o **seu usuário de domínio** — sem usuário/senha de aplicação.

## Caso de uso corporativo

- Consultar **tabelas**, **views** e **metadados de procedures** via IA (Gemini, Cursor, Claude)
- Ler definições SQL de views/procedures sem executar `EXEC`
- Bloquear DDL/DML/`EXEC`/comandos perigosos no validador
- Respeitar permissões já existentes do usuário de rede no banco

## Requisitos

- Windows com usuário de rede autorizado no SQL Server
- Python 3.10+
- ODBC Driver 17 ou 18 for SQL Server

## Instalação

```powershell
cd C:\Users\HOME\MCP-SQL
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
```

## Configuração no Gemini CLI

Edite `%USERPROFILE%\.gemini\settings.json`:

```json
{
  "mcpServers": {
    "sqlserver": {
      "command": "C:\\Users\\HOME\\MCP-SQL\\.venv\\Scripts\\mcp-sqlserver.exe",
      "env": {
        "MSSQL_SERVER": "NOME_DO_SERVIDOR\\INSTANCIA",
        "MSSQL_DATABASE": "NomeDoBanco",
        "MSSQL_READONLY": "true",
        "MSSQL_APPLICATION_INTENT_READONLY": "true",
        "MSSQL_MAX_ROWS": "200"
      }
    }
  }
}
```

Alternativa sem instalar o entry point:

```json
{
  "mcpServers": {
    "sqlserver": {
      "command": "C:\\Users\\HOME\\MCP-SQL\\.venv\\Scripts\\python.exe",
      "args": ["-m", "mcp_sqlserver"],
      "env": {
        "MSSQL_SERVER": "NOME_DO_SERVIDOR\\INSTANCIA",
        "MSSQL_DATABASE": "NomeDoBanco"
      }
    }
  }
}
```

Reinicie o Gemini CLI e valide com `/mcp`.

## Um servidor, vários bancos — preciso configurar um a um?

**Não.** Você configura **um MCP por servidor/ambiente**, não por banco de dados.

Exemplo: o servidor HML `SRVSQL01\HML` com 10 bases (`Vendas`, `Financeiro`, `RH`...) exige **apenas uma entrada** no `settings.json`:

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "C:\\Users\\HOME\\MCP-SQL\\.venv\\Scripts\\mcp-sqlserver.exe",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

Depois, o Gemini usa as ferramentas passando o banco desejado:

1. `listar_bancos()` → descobre todas as bases que seu usuário de rede enxerga
2. `listar_tabelas(database="Vendas")` → explora um banco específico
3. `executar_consulta(sql="SELECT TOP 10 * FROM ...", database="Financeiro")` → consulta em outro banco

O `MSSQL_DATABASE` é só o **banco padrão** quando a ferramenta não recebe `database`. Coloque `master` (ou o banco mais usado).

**Quando criar mais de uma entrada no settings.json:**

| Situação | Entradas necessárias |
|---|---|
| HML com N bases no mesmo servidor | **1** (`sqlserver-hml`) |
| DEV + HML + PROD (servidores diferentes) | **1 por ambiente** |
| Mesmo servidor, usuários de rede diferentes | **1 por perfil** (raro) |

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `MSSQL_SERVER` | — | Servidor/instância (obrigatória) |
| `MSSQL_DATABASE` | `master` | Banco padrão |
| `MSSQL_DRIVER` | `ODBC Driver 17 for SQL Server` | Driver ODBC |
| `MSSQL_READONLY` | `true` | Bloqueia DDL/DML/`EXEC` |
| `MSSQL_APPLICATION_INTENT_READONLY` | `true` | Usa `ApplicationIntent=ReadOnly` na connection string |
| `MSSQL_MAX_ROWS` | `200` | Limite de linhas retornadas |
| `MSSQL_CONNECTION_TIMEOUT` | `15` | Timeout de conexão (segundos) |

## Ferramentas MCP

| Ferramenta | Descrição |
|---|---|
| `usuario_conectado` | Login Windows, usuário de banco e modo MCP |
| `listar_bancos` | Bancos visíveis |
| `listar_tabelas` | Tabelas base |
| `listar_views` | Views |
| `listar_procedures` | Procedures e functions (metadados) |
| `descrever_tabela` | Colunas de tabela |
| `descrever_view` | Colunas de view |
| `descrever_procedure` | Parâmetros de procedure/function |
| `obter_definicao_sql` | Script SQL da view/procedure/function |
| `consultar_view` | `SELECT TOP` seguro em uma view |
| `executar_consulta` | `SELECT`/`WITH` validado |

> **Importante:** procedures **não são executadas** (`EXEC` bloqueado). O foco é
> leitura de dados e metadados — adequado para ambiente corporativo.

## Teste rápido

```powershell
$env:MSSQL_SERVER = "SRVSQL01\PROD"
$env:MSSQL_DATABASE = "MeuBanco"
.\.venv\Scripts\python.exe -c "from mcp_sqlserver.server import usuario_conectado; print(usuario_conectado())"
```

## Testes automatizados

```powershell
.\.venv\Scripts\python.exe -m pytest
```

## Publicação (roadmap)

Este projeto está em **Python** e será publicado no **PyPI** (`pip install mcp-sqlserver`).

Se a meta for **NuGet** (.NET), o caminho recomendado é um pacote irmão em C# usando
[ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol), reutilizando
a mesma política de somente leitura e autenticação Windows (`Integrated Security=true`).

Passos previstos para PyPI:

```powershell
python -m pip install build twine
python -m build
twine upload dist/*
```

## Segurança

- O validador SQL é uma camada extra; as permissões reais vêm do SQL Server
- Não desative `MSSQL_READONLY` em produção corporativa
- O que o usuário não vê no SSMS, o MCP também não verá
