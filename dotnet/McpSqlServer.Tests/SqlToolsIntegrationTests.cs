using McpSqlServer.Services;
using McpSqlServer.Tools;
using Xunit;

namespace McpSqlServer.Tests;

public sealed class IntegrationFixture : IDisposable
{
    public const string DatabaseName = "McpSqlServerTest";

    public IntegrationFixture()
    {
        var connectionString = Environment.GetEnvironmentVariable("MCPMSSQL_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("MCPMSSQL_TEST_CONNECTION_STRING não definida.");

        Environment.SetEnvironmentVariable("MCPMSSQL_CONNECTION_STRING", WithDatabase(connectionString, "master"));
        Environment.SetEnvironmentVariable("MSSQL_READONLY", "true");
        Environment.SetEnvironmentVariable("MSSQL_ENABLE_PERFORMANCE_DMVS", "true");

        Config = new McpConfig();
        Executor = new SqlExecutor(Config);
        Tools = new SqlTools(Config, Executor);

        Seed();
    }

    public McpConfig Config { get; }
    public SqlExecutor Executor { get; }
    public SqlTools Tools { get; }

    public void Dispose()
    {
    }

    private void Seed()
    {
        Executor.Execute($"IF DB_ID('{DatabaseName}') IS NULL CREATE DATABASE [{DatabaseName}]", "master");

        var seedPath = FindSeedPath();
        var batches = File.ReadAllText(seedPath)
            .Split("\nGO\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var batch in batches)
        {
            var result = Executor.Execute(batch, DatabaseName);
            if (result.StartsWith("Erro SQL", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Falha ao executar seed.sql: {result}");
            }
        }
    }

    private static string FindSeedPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "integration", "seed.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Arquivo tests/integration/seed.sql não encontrado.");
    }

    private static string WithDatabase(string connectionString, string database)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase)
                && !part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase));
        return string.Join(";", parts) + $";Database={database};";
    }
}

public class SqlToolsIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public SqlToolsIntegrationTests(IntegrationFixture fixture) => _fixture = fixture;

    [IntegrationFact]
    public void ListarIndices_com_filtro_tabela_nao_gera_erro_137()
    {
        var result = _fixture.Tools.ListarIndices(
            database: IntegrationFixture.DatabaseName,
            schema: "dbo",
            tabela: "Pedidos");

        Assert.DoesNotContain("@tabelaGROUP", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Erro SQL (137)", result, StringComparison.Ordinal);
        Assert.Contains("IX_Pedidos_Status", result, StringComparison.Ordinal);
    }

    [IntegrationFact]
    public void ListarFksSemIndice_retorna_fk_de_pedidos()
    {
        var result = _fixture.Tools.ListarFksSemIndice(
            database: IntegrationFixture.DatabaseName,
            schema: "dbo");

        Assert.DoesNotContain("Erro SQL", result, StringComparison.Ordinal);
        Assert.True(
            result.Contains("ClienteId", StringComparison.Ordinal)
                || result.Contains("FK_Pedidos_Clientes", StringComparison.Ordinal),
            result);
    }

    [IntegrationFact]
    public void ConsultasLentas_sem_permissao_retorna_mensagem_e_nao_lanca_excecao()
    {
        var result = _fixture.Tools.ConsultasLentas(
            database: IntegrationFixture.DatabaseName,
            top: 5);

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("unhandled exception", result, StringComparison.OrdinalIgnoreCase);
    }

    [IntegrationFact]
    public void EstimarPlanoConsulta_retorna_xml_ou_mensagem_controlada()
    {
        var result = _fixture.Tools.EstimarPlanoConsulta(
            sql: "SELECT Id FROM dbo.Pedidos WHERE Status = 'A'",
            database: IntegrationFixture.DatabaseName);

        Assert.DoesNotContain("Erro SQL (137)", result, StringComparison.Ordinal);
        Assert.True(
            result.Contains("ShowPlanXML", StringComparison.OrdinalIgnoreCase)
                || result.Contains("Plano", StringComparison.OrdinalIgnoreCase)
                || result.Contains("Erro", StringComparison.OrdinalIgnoreCase),
            result);
    }
}

public class SqlExecutorIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public SqlExecutorIntegrationTests(IntegrationFixture fixture) => _fixture = fixture;

    [IntegrationFact]
    public void Execute_com_sql_invalido_retorna_erro_formatado()
    {
        var result = _fixture.Executor.Execute(
            "SELECT * FROM dbo.TabelaInexistente",
            IntegrationFixture.DatabaseName);

        Assert.StartsWith("Erro SQL", result, StringComparison.Ordinal);
    }
}
