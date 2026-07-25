# Instalação

## Requisitos

- **Windows** com usuário de rede autorizado no SQL Server
- **ODBC Driver 17 ou 18 for SQL Server** — [download Microsoft](https://learn.microsoft.com/sql/connect/odbc/download-odbc-driver-for-sql-server)
- Acesso ao SQL Server com autenticação integrada (Windows)

---

## Opção 1 — NuGet (recomendado para corporativo)

Pacote global .NET que expõe o comando `mcp-sqlserver` no PATH.

### Instalar

```powershell
dotnet tool install --global McpSqlServer
```

### Atualizar

```powershell
dotnet tool update --global McpSqlServer
```

### Verificar

```powershell
mcp-sqlserver --version
where.exe mcp-sqlserver
```

### Testar conexão

```powershell
$env:MSSQL_SERVER = "SRVSQL01\HML"
$env:MSSQL_DATABASE = "master"
$env:MSSQL_READONLY = "true"

# O servidor MCP usa stdio — para testar conexão SQL diretamente:
sqlcmd -S $env:MSSQL_SERVER -E -Q "SELECT SUSER_SNAME() AS login_windows"
```

> Se `sqlcmd` retornar `DOMINIO\seu.usuario`, a autenticação Windows está ok.

### Desinstalar

```powershell
dotnet tool uninstall --global McpSqlServer
```

---

## Opção 2 — PyPI (Python)

### Instalar

```powershell
pip install mcp-sqlserver
```

### Instalar versão específica

```powershell
pip install mcp-sqlserver==0.2.0
```

### Verificar

```powershell
mcp-sqlserver --help
python -c "import mcp_sqlserver; print(mcp_sqlserver.__version__)"
```

### Testar conexão

```powershell
$env:MSSQL_SERVER = "SRVSQL01\HML"
$env:MSSQL_DATABASE = "MeuBanco"

python -c "from mcp_sqlserver.server import usuario_conectado; print(usuario_conectado())"
```

Saída esperada:

```
login_windows | usuario_banco | banco_atual | servidor | modo_mcp
--------------|---------------|-------------|----------|---------------
DOMINIO\voce  | dbo           | MeuBanco    | SRVSQL01 | somente_leitura
```

---

## Opção 3 — Desenvolvimento (clone do repositório)

```powershell
git clone https://github.com/SouzaMatheus-dev/mcp-sqlserver.git
cd mcp-sqlserver

python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
```

Executar localmente:

```powershell
$env:MSSQL_SERVER = "SRVSQL01\HML"
.\.venv\Scripts\mcp-sqlserver.exe
```

---

## Próximo passo

Após instalar, configure o cliente MCP:

→ [Configuração MCP](configuracao-mcp.md)
