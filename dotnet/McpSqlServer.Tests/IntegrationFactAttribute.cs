using Xunit;

namespace McpSqlServer.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCPMSSQL_TEST_CONNECTION_STRING")))
        {
            Skip = "Defina MCPMSSQL_TEST_CONNECTION_STRING para executar testes de integração.";
        }
    }
}
