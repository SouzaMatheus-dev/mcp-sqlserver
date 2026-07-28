# McpSqlServer

Servidor **MCP especialista em SQL Server** com autenticação Windows integrada e **modo somente leitura** por padrão.

Conecte **Gemini**, **Cursor**, **Claude Desktop** ou **VS Code** ao SQL Server usando o seu **usuário de rede** — sem credenciais de aplicação.

Repositório: https://github.com/SouzaMatheus-dev/mcp-sqlserver

## Instalação

### Global tool (.NET 8+)

```powershell
dotnet tool install --global McpSqlServer
dotnet tool update --global McpSqlServer
```

### dotnet dnx (requer .NET 10 SDK)

```powershell
dotnet dnx McpSqlServer --yes --source https://api.nuget.org/v3/index.json
```

> Em máquinas corporativas com .NET 8/9, prefira a **global tool** (`mcp-sqlserver`).

## Configuração MCP

### Global tool — Gemini / Cursor

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

### dotnet dnx — Gemini

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

Reinicie o cliente MCP e valide com `usuario_conectado`.

## Um servidor, vários bancos

Configure **uma entrada por ambiente** (DEV, HML, PROD). Use o parâmetro `database` nas ferramentas:

```
listar_bancos()
listar_tabelas(database="Vendas")
executar_consulta(sql="SELECT TOP 10 * FROM dbo.Pedidos", database="Financeiro")
```

## Ferramentas MCP (24)

### Conexão e inventário

| Ferramenta | Descrição |
|---|---|
| `usuario_conectado` | Login Windows, banco atual e modo MCP |
| `listar_bancos` | Bancos visíveis no servidor |
| `listar_tabelas` | Tabelas base |
| `listar_views` | Views |
| `listar_procedures` | Procedures e functions (metadados) |
| `resumir_banco` | Contagens por tipo e TOP 20 maiores tabelas |

### Metadados e dicionário de dados

| Ferramenta | Descrição |
|---|---|
| `descrever_tabela` | Colunas, tipos, PK |
| `descrever_view` | Colunas de view |
| `descrever_procedure` | Parâmetros de procedure/function |
| `obter_definicao_sql` | Script SQL do objeto |
| `obter_documentacao_objeto` | MS_Description (objeto e colunas) |

### Relacionamentos e impacto

| Ferramenta | Descrição |
|---|---|
| `listar_chaves_estrangeiras` | Foreign keys para montar JOINs |
| `listar_indices` | Índices, unique e PK |
| `listar_dependencias` | Quem referencia / é referenciado por um objeto |

### Descoberta

| Ferramenta | Descrição |
|---|---|
| `buscar_coluna` | Busca colunas pelo nome (LIKE) |
| `buscar_objeto` | Busca tabelas, views e procedures |
| `buscar_texto_sql` | Busca texto em views/procedures/functions |

### Entender os dados

| Ferramenta | Descrição |
|---|---|
| `amostrar_tabela` | Amostra linhas (SELECT TOP seguro) |
| `perfil_coluna` | Nulos, distintos, min/max e valores frequentes |
| `consultar_view` | SELECT TOP em view |
| `executar_consulta` | SELECT/WITH validado |

### Performance (opt-in)

Requer `MSSQL_ENABLE_PERFORMANCE_DMVS=true`. As demais ferramentas funcionam normalmente sem essa flag.

| Ferramenta | Descrição |
|---|---|
| `consultas_lentas` | TOP consultas por tempo médio (DMVs) |
| `indices_nao_utilizados` | Índices sem seeks/scans/lookups |
| `estimar_plano_consulta` | Plano SHOWPLAN_XML para SELECT |

> Procedures **não são executadas** (`EXEC` bloqueado). Foco em leitura corporativa.

## Fluxo sugerido para devs

1. `resumir_banco` → visão geral
2. `listar_chaves_estrangeiras` + `listar_indices` → entender JOINs
3. `buscar_texto_sql` / `buscar_coluna` → achar lógica e campos
4. `amostrar_tabela` + `perfil_coluna` → entender os dados
5. `listar_dependencias` → avaliar impacto de mudanças
6. *(opt-in)* `consultas_lentas` / `estimar_plano_consulta` → propor melhorias

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `MCPMSSQL_CONNECTION_STRING` | — | Connection string completa (**recomendado**) |
| `MSSQL_SERVER` | — | Servidor/instância (alternativa) |
| `MSSQL_DATABASE` | `master` | Banco padrão |
| `MSSQL_READONLY` | `true` | Bloqueia DDL, DML e EXEC |
| `MSSQL_APPLICATION_INTENT_READONLY` | `true` | ApplicationIntent=ReadOnly |
| `MSSQL_MAX_ROWS` | `200` | Limite de linhas retornadas |
| `MSSQL_CONNECTION_TIMEOUT` | `15` | Timeout de conexão (segundos) |
| `MSSQL_ENABLE_PERFORMANCE_DMVS` | `false` | Habilita ferramentas de performance |

## Segurança

- Autenticação via usuário de rede (`Integrated Security=SSPI`)
- Validador SQL bloqueia comandos de escrita
- Permissões reais vêm do SQL Server — o MCP não eleva privilégios
- Mantenha `MSSQL_READONLY=true` em produção

## Documentação completa

- Instalação: https://github.com/SouzaMatheus-dev/mcp-sqlserver/blob/main/docs/instalacao.md
- Configuração MCP: https://github.com/SouzaMatheus-dev/mcp-sqlserver/blob/main/docs/configuracao-mcp.md
- Exemplos de uso: https://github.com/SouzaMatheus-dev/mcp-sqlserver/blob/main/docs/exemplos-uso.md

## Licença

MIT
