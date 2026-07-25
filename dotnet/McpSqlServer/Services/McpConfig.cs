using Microsoft.Data.SqlClient;

namespace McpSqlServer.Services;

public sealed class McpConfig
{
    private readonly Lazy<string> _baseConnectionString;

    public McpConfig()
    {
        _baseConnectionString = new Lazy<string>(BuildBaseConnectionString);
    }

    public bool ReadOnly => GetBool("MSSQL_READONLY", defaultValue: true);

    public int MaxRows => GetInt("MSSQL_MAX_ROWS", defaultValue: 200);

    public int ConnectionTimeoutSeconds => GetInt("MSSQL_CONNECTION_TIMEOUT", defaultValue: 15);

    public string GetConnectionString(string? database = null)
    {
        var builder = new SqlConnectionStringBuilder(_baseConnectionString.Value)
        {
            ConnectTimeout = ConnectionTimeoutSeconds,
        };

        if (ReadOnly && GetBool("MSSQL_APPLICATION_INTENT_READONLY", defaultValue: true))
        {
            builder.ApplicationIntent = ApplicationIntent.ReadOnly;
        }

        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.InitialCatalog = database;
        }

        return builder.ConnectionString;
    }

    private static string BuildBaseConnectionString()
    {
        var connectionString = FirstNonEmpty(
            Environment.GetEnvironmentVariable("MCPMSSQL_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("MCP_SQLSERVER_CONNECTION_STRING"));

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var server = Environment.GetEnvironmentVariable("MSSQL_SERVER");
        if (string.IsNullOrWhiteSpace(server))
        {
            throw new InvalidOperationException(
                "Defina MCPMSSQL_CONNECTION_STRING ou MSSQL_SERVER no ambiente.");
        }

        var database = Environment.GetEnvironmentVariable("MSSQL_DATABASE") ?? "master";
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
        };

        return builder.ConnectionString;
    }

    private static bool GetBool(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
