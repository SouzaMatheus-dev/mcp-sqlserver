# Exemplos de uso

Fluxos típicos ao conversar com o Gemini/Cursor usando o MCP-SQLServer.

---

## 1. Validar autenticação

Peça ao assistente:

> Use a ferramenta `usuario_conectado` para verificar com qual login estou conectado.

Resposta esperada:

```
login_windows     | usuario_banco | banco_atual | servidor | modo_mcp
------------------|---------------|-------------|----------|----------------
DOMINIO\j.silva   | dbo           | master      | SRVSQL01 | somente_leitura
```

---

## 2. Explorar bancos no HML (multi-banco)

Servidor HML com bases `Vendas`, `Financeiro`, `RH`:

```
Assistente, liste os bancos disponíveis no HML.
→ listar_bancos()

Agora liste as tabelas do banco Vendas.
→ listar_tabelas(database="Vendas")

Quais views existem no banco Financeiro?
→ listar_views(database="Financeiro")
```

---

## 3. Descrever estrutura de tabela e view

```
Descreva a tabela dbo.Pedidos no banco Vendas.
→ descrever_tabela(tabela="Pedidos", database="Vendas", schema="dbo")

Descreva a view dbo.vwClientesAtivos no banco Vendas.
→ descrever_view(view="vwClientesAtivos", database="Vendas", schema="dbo")
```

---

## 4. Consultar dados (somente leitura)

### Via view

```
Consulte os 50 primeiros registros da view dbo.vwPedidosResumo no banco Vendas.
→ consultar_view(view="vwPedidosResumo", database="Vendas", schema="dbo", top=50)
```

### Via SQL livre (SELECT validado)

```
Execute: SELECT TOP 10 Id, Cliente, Valor, DataPedido
         FROM dbo.Pedidos
         WHERE DataPedido >= '2026-01-01'
         ORDER BY DataPedido DESC
         no banco Vendas.
→ executar_consulta(
    sql="SELECT TOP 10 Id, Cliente, Valor, DataPedido FROM dbo.Pedidos WHERE DataPedido >= '2026-01-01' ORDER BY DataPedido DESC",
    database="Vendas"
  )
```

### Com CTE (WITH)

```
→ executar_consulta(
    sql="WITH top_clientes AS (SELECT ClienteId, SUM(Valor) AS total FROM dbo.Pedidos GROUP BY ClienteId) SELECT TOP 5 * FROM top_clientes ORDER BY total DESC",
    database="Vendas"
  )
```

---

## 5. Explorar procedures (metadados, sem EXEC)

```
Liste as procedures do schema dbo no banco Financeiro.
→ listar_procedures(database="Financeiro", schema="dbo")

Descreva os parâmetros da procedure dbo.spRelatorioMensal.
→ descrever_procedure(procedure="spRelatorioMensal", database="Financeiro", schema="dbo")

Mostre o script SQL da procedure dbo.spRelatorioMensal.
→ obter_definicao_sql(objeto="spRelatorioMensal", database="Financeiro", schema="dbo")
```

> `EXEC` é **bloqueado** em modo corporativo. Use as ferramentas acima para entender
> a lógica sem executar a procedure.

---

## 6. Comandos bloqueados (modo corporativo)

Com `MSSQL_READONLY=true` (padrão), estes comandos são rejeitados:

```sql
-- Bloqueados
INSERT INTO dbo.Log VALUES (...)
UPDATE dbo.Pedidos SET Status = 'X'
DELETE FROM dbo.Temp
EXEC dbo.spRelatorioMensal @Mes=1
DROP TABLE dbo.Temp
SELECT * INTO backup FROM dbo.Pedidos
```

Resposta do MCP:

```
Bloqueado: em modo corporativo somente leitura só são permitidas consultas que começam com SELECT ou WITH.
```

---

## 7. Cenário completo — analista no HML

Prompt sugerido para o Gemini:

```
Conectado ao sqlserver-hml. Preciso entender a base Vendas:
1. Liste os bancos disponíveis
2. Liste tabelas e views do banco Vendas
3. Descreva a tabela dbo.Pedidos
4. Consulte TOP 5 registros da view dbo.vwPedidosResumo
5. Liste procedures do schema dbo e mostre a definição de spRelatorioVendas se existir
```

O assistente encadeia as ferramentas automaticamente.

---

## 8. Comparar ambientes (DEV vs HML)

Com três entradas no `settings.json` (`sqlserver-dev`, `sqlserver-hml`, `sqlserver-prod`):

```
No sqlserver-dev, liste as tabelas do banco Vendas.
No sqlserver-hml, faça o mesmo e compare.
```

Cada entrada MCP aponta para um `MSSQL_SERVER` diferente.

---

## 9. Variáveis de ambiente — exemplos

### Leitura conservadora (produção)

```json
"env": {
  "MSSQL_SERVER": "SRVSQL01\\PROD",
  "MSSQL_DATABASE": "master",
  "MSSQL_READONLY": "true",
  "MSSQL_APPLICATION_INTENT_READONLY": "true",
  "MSSQL_MAX_ROWS": "100"
}
```

### Homologação (mais linhas)

```json
"env": {
  "MSSQL_SERVER": "SRVSQL01\\HML",
  "MSSQL_DATABASE": "master",
  "MSSQL_READONLY": "true",
  "MSSQL_MAX_ROWS": "500"
}
```

---

## Próximo passo

→ [Publicação NuGet](publicacao-nuget.md) — como publicar e consumir o pacote .NET
