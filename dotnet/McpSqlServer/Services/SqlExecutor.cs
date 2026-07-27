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

    public string ExecuteShowPlan(string sql, string? database = null)
    {
        using var connection = new SqlConnection(config.GetConnectionString(database));
        connection.Open();

        using (var on = connection.CreateCommand())
        {
            on.CommandText = "SET SHOWPLAN_XML ON";
            on.ExecuteNonQuery();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = config.ConnectionTimeoutSeconds;

            using var reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                return "Plano de execução não retornado.";
            }

            var parts = new List<string>();
            var count = 0;
            while (count < config.MaxRows && reader.Read())
            {
                parts.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
                count++;
            }

            if (reader.Read())
            {
                parts.Add($"... plano truncado em {config.MaxRows} fragmento(s) ...");
            }

            return parts.Count > 0
                ? string.Join(Environment.NewLine, parts)
                : "Plano de execução vazio.";
        }
        finally
        {
            using var off = connection.CreateCommand();
            off.CommandText = "SET SHOWPLAN_XML OFF";
            off.ExecuteNonQuery();
        }
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
