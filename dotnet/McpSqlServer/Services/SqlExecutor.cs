using System.Text;
using Microsoft.Data.SqlClient;

namespace McpSqlServer.Services;

public sealed class SqlExecutor(McpConfig config)
{
    public string Execute(string sql, string? database = null, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(config.GetConnectionString(database));
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = config.ConnectionTimeoutSeconds;
        command.Parameters.AddRange(parameters);

        using var reader = command.ExecuteReader();
        return ResultFormatter.Format(reader, config.MaxRows);
    }
}

internal static class ResultFormatter
{
    public static string Format(SqlDataReader reader, int maxRows)
    {
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToArray();

        var header = string.Join(" | ", columns);
        var output = new StringBuilder();
        output.AppendLine(header);
        output.AppendLine(new string('-', header.Length));

        var count = 0;
        while (count < maxRows && reader.Read())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? "NULL" : reader.GetValue(index)?.ToString() ?? string.Empty);
            output.AppendLine(string.Join(" | ", values));
            count++;
        }

        var truncated = reader.Read();
        if (truncated)
        {
            output.AppendLine($"... resultado truncado em {maxRows} linhas ...");
        }

        output.AppendLine($"({count} linha(s) exibida(s))");
        return output.ToString().TrimEnd();
    }
}
