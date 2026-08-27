using McpSqlServer.Services;
using Xunit;

namespace McpSqlServer.Tests;

public class ReadOnlyValidatorTests
{
    [Theory]
    [InlineData("SELECT 1")]
    [InlineData("WITH cte AS (SELECT 1 AS n) SELECT * FROM cte")]
    [InlineData("SELECT * FROM dbo.Clientes WHERE id = 1")]
    public void Validate_permite_consultas_leitura(string sql)
    {
        Assert.Null(ReadOnlyValidator.Validate(sql));
    }

    [Theory]
    [InlineData("DELETE FROM tabela")]
    [InlineData("INSERT INTO tabela VALUES (1)")]
    [InlineData("SELECT * INTO backup FROM tabela")]
    [InlineData("EXEC sp_help")]
    [InlineData("SELECT 1; DROP TABLE t")]
    public void Validate_bloqueia_comandos_perigosos(string sql)
    {
        Assert.NotNull(ReadOnlyValidator.Validate(sql));
    }

    [Fact]
    public void Validate_bloqueia_bypass_por_comentario()
    {
        Assert.NotNull(ReadOnlyValidator.Validate("/* ok */ SELECT 1; DELETE FROM t"));
    }
}
