# MCP-SQL — Servidor MCP para SQL Server com autenticação Windows

Servidor MCP que conecta ao SQL Server usando **autenticação integrada do Windows**
(`Trusted_Connection=yes`). Como o processo do MCP roda na sua máquina com a sua
sessão de rede, o SQL Server enxerga a conexão como o **seu usuário de domínio** —
não é necessário criar usuários de aplicação nem informar senha.

## Requisitos

- Windows com usuário de rede que já tem acesso ao SQL Server
- Python 3.10+
- ODBC Driver 17 ou 18 for SQL Server

## Instalação

```powershell
cd C:\Users\HOME\MCP-SQL
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

## Configuração no Gemini CLI

Edite o arquivo `%USERPROFILE%\.gemini\settings.json` (crie se não existir) e
adicione o bloco `mcpServers`:

```json
{
  "mcpServers": {
    "sqlserver": {
      "command": "C:\\Users\\HOME\\MCP-SQL\\.venv\\Scripts\\python.exe",
      "args": ["C:\\Users\\HOME\\MCP-SQL\\server.py"],
      "env": {
        "MSSQL_SERVER": "NOME_DO_SERVIDOR\\INSTANCIA",
        "MSSQL_DATABASE": "NomeDoBanco",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

Ajuste `MSSQL_SERVER` (ex.: `SRVSQL01` ou `SRVSQL01\PROD` ou `servidor,1433`)
e `MSSQL_DATABASE` para o seu ambiente. Depois reinicie o Gemini CLI e verifique
com `/mcp` se o servidor `sqlserver` apareceu.

> A mesma configuração funciona em outros clientes MCP (Cursor, Claude Desktop,
> VS Code etc.) — basta usar o mesmo `command`/`args`/`env` no arquivo de
> configuração de MCP de cada um.

## Variáveis de ambiente

| Variável | Obrigatória | Descrição |
|---|---|---|
| `MSSQL_SERVER` | Sim | Servidor/instância do SQL Server |
| `MSSQL_DATABASE` | Não | Banco padrão (default: `master`) |
| `MSSQL_DRIVER` | Não | Driver ODBC (default: `ODBC Driver 17 for SQL Server`) |
| `MSSQL_READONLY` | Não | `true` (padrão) bloqueia tudo que não for SELECT/WITH |

## Ferramentas disponíveis

- `usuario_conectado` — mostra com qual login a conexão chegou no SQL Server (bom para validar a autenticação)
- `listar_bancos` — lista os bancos do servidor
- `listar_tabelas` — lista tabelas/views de um banco, com filtro opcional de schema
- `descrever_tabela` — colunas, tipos, nulabilidade e chave primária
- `executar_consulta` — executa SQL (somente leitura por padrão; resultados limitados a 200 linhas)

## Teste rápido de conexão (fora do MCP)

```powershell
$env:MSSQL_SERVER = "SRVSQL01\PROD"
.\.venv\Scripts\python.exe -c "import server; print(server.usuario_conectado())"
```

Se retornar o seu login de domínio (`DOMINIO\seu.usuario`), a autenticação
integrada está funcionando.

## Observações

- Para ambientes de Dev/Homologação/Produção diferentes, você pode registrar o
  mesmo servidor MCP mais de uma vez (ex.: `sqlserver-dev`, `sqlserver-hml`),
  mudando apenas o `MSSQL_SERVER`/`MSSQL_DATABASE` no `env` de cada um.
- O acesso respeita exatamente as permissões do seu usuário de rede no banco:
  o que você não pode ver pelo SSMS, o MCP também não verá.
