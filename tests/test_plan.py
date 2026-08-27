from mcp_sqlserver.plan import extrair_sugestoes_missing_index

SAMPLE_PLAN = """
<ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan">
  <MissingIndexGroup Impact="95.5">
    <MissingIndex Database="[Vendas]" Schema="[dbo]" Table="[Pedidos]">
      <ColumnGroup Usage="EQUALITY">
        <Column Name="[Status]" ColumnId="1" />
      </ColumnGroup>
      <ColumnGroup Usage="INCLUDE">
        <Column Name="[ClienteId]" ColumnId="2" />
      </ColumnGroup>
    </MissingIndex>
  </MissingIndexGroup>
</ShowPlanXML>
"""


def test_extrair_sugestoes_missing_index_encontra_colunas():
    resultado = extrair_sugestoes_missing_index(SAMPLE_PLAN)
    assert "MissingIndex" in resultado or "Sugestões" in resultado
    assert "Pedidos" in resultado
    assert "Status" in resultado
    assert "ClienteId" in resultado


def test_extrair_sugestoes_missing_index_sem_sugestao():
    assert "Nenhuma sugestão" in extrair_sugestoes_missing_index("<ShowPlanXML/>")
