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

## 6. Relacionamentos e dicionário de dados (v0.5.0)

```
Liste as foreign keys da tabela dbo.Pedidos no banco Vendas.
→ listar_chaves_estrangeiras(database="Vendas", schema="dbo", tabela="Pedidos")

Onde existe a coluna ClienteId?
→ buscar_coluna(termo="ClienteId", database="Vendas")

Busque objetos com "Pedido" no nome.
→ buscar_objeto(termo="Pedido", database="Vendas")

Qual a documentação MS_Description da tabela dbo.Pedidos?
→ obter_documentacao_objeto(objeto="Pedidos", database="Vendas", schema="dbo")
```

---

## 7. Índices, dependências e visão geral (v0.6.0)

```
Quais índices existem na tabela dbo.Pedidos?
→ listar_indices(database="Vendas", schema="dbo", tabela="Pedidos")

O que depende da view dbo.vwPedidosResumo?
→ listar_dependencias(objeto="vwPedidosResumo", database="Vendas", schema="dbo", direcao="referenciado_por")

De quais objetos a procedure dbo.spRelatorio depende?
→ listar_dependencias(objeto="spRelatorio", database="Vendas", schema="dbo", direcao="referencia")

Me dê um resumo do banco Vendas.
→ resumir_banco(database="Vendas")
```

---

## 8. Entender os dados (v0.7.0)

```
Amostre 10 linhas da tabela dbo.Pedidos.
→ amostrar_tabela(tabela="Pedidos", database="Vendas", schema="dbo", top=10)

Qual o perfil da coluna Status na tabela dbo.Pedidos?
→ perfil_coluna(tabela="Pedidos", coluna="Status", database="Vendas", schema="dbo")

Onde aparece a palavra "ClienteId" no código SQL do banco?
→ buscar_texto_sql(termo="ClienteId", database="Vendas")
```

---

## 9. Tuning estrutural (v0.9.0 — sem DMVs de servidor)

```
Quais FKs não têm índice de suporte?
→ listar_fks_sem_indice(database="Vendas", schema="dbo")

As colunas Status,ClienteId da tabela Pedidos têm índice?
→ analisar_cobertura_indice(tabela="Pedidos", colunas="Status,ClienteId", database="Vendas", schema="dbo")

Existem índices redundantes no banco?
→ comparar_indices_redundantes(database="Vendas", schema="dbo")

Quais colunas em tabelas grandes são candidatas a índice?
→ listar_colunas_candidatas_indice(database="Vendas", schema="dbo", min_linhas=1000)

Meça IO e tempo desta consulta:
→ medir_consulta(sql="SELECT * FROM dbo.Pedidos WHERE Status = 'A'", database="Vendas")

Estime o plano e extraia sugestões de índice:
→ extrair_sugestoes_plano(sql="SELECT * FROM dbo.Pedidos WHERE Status = 'A'", database="Vendas")

Estime o plano completo (XML):
→ estimar_plano_consulta(sql="SELECT * FROM dbo.Pedidos WHERE Status = 'A'", database="Vendas")
```

---

## 10. Performance em runtime (v0.8.0, opt-in)

Requer `MSSQL_ENABLE_PERFORMANCE_DMVS=true` — somente para estatísticas globais do servidor.

```
Quais as consultas mais lentas no banco Vendas?
→ consultas_lentas(database="Vendas", top=20)

Quais índices não estão sendo usados?
→ indices_nao_utilizados(database="Vendas", schema="dbo")
```

Exemplo de env:

```json
"MSSQL_ENABLE_PERFORMANCE_DMVS": "true"
```

---

## 11. Comandos bloqueados (modo corporativo)

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

## 12. Cenário completo — analista no HML

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

## 13. Comparar ambientes (DEV vs HML)

Com três entradas no `settings.json` (`sqlserver-dev`, `sqlserver-hml`, `sqlserver-prod`):

```
No sqlserver-dev, liste as tabelas do banco Vendas.
No sqlserver-hml, faça o mesmo e compare.
```

Cada entrada MCP aponta para um `MSSQL_SERVER` diferente.

---

## 14. Variáveis de ambiente — exemplos

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
