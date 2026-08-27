using Xunit;

namespace McpSqlServer.Tests;

public class RegressionTests
{
    [Fact]
    public void ListarIndices_nao_concatena_parametro_com_group()
    {
        var source = File.ReadAllText(FindRepoFile("dotnet", "McpSqlServer", "Tools", "SqlTools.cs"));
        Assert.Contains(@" AND t.name = @tabela\n", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@tabelaGROUP", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ListarColunasCandidatas_nao_concatena_parametro_com_group()
    {
        var source = File.ReadAllText(FindRepoFile("dotnet", "McpSqlServer", "Tools", "SqlTools.cs"));
        Assert.Contains(@" AND SCHEMA_NAME(t.schema_id) = @schema\n", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@schemaGROUP", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlExecutor_trata_sql_exception()
    {
        var source = File.ReadAllText(FindRepoFile("dotnet", "McpSqlServer", "Services", "SqlExecutor.cs"));
        Assert.Contains("catch (SqlException", source, StringComparison.Ordinal);
        Assert.Contains("SqlErrorFormatter.Format", source, StringComparison.Ordinal);
    }

    private static string FindRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(string.Join(Path.DirectorySeparatorChar, parts));
    }
}
