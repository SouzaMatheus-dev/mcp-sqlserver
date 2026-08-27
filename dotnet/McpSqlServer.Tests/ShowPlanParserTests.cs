using McpSqlServer.Services;
using Xunit;

namespace McpSqlServer.Tests;

public class ShowPlanParserTests
{
    private const string SamplePlan = """
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
        """;

    [Fact]
    public void ExtractMissingIndexSuggestions_encontra_colunas()
    {
        var result = ShowPlanParser.ExtractMissingIndexSuggestions(SamplePlan);
        Assert.Contains("Pedidos", result, StringComparison.Ordinal);
        Assert.Contains("Status", result, StringComparison.Ordinal);
        Assert.Contains("ClienteId", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractMissingIndexSuggestions_sem_sugestao()
    {
        var result = ShowPlanParser.ExtractMissingIndexSuggestions("<ShowPlanXML/>");
        Assert.Contains("Nenhuma sugestão", result, StringComparison.Ordinal);
    }
}
