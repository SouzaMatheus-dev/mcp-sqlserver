using Microsoft.Data.SqlClient;

namespace McpSqlServer.Services;

internal static class SqlErrorFormatter
{
    public static string Format(SqlException exception)
    {
        var message = exception.Message.Trim();
        return exception.Number switch
        {
            229 or 297 or 300 or 262 =>
                "Erro de permissão (SQL "
                + exception.Number
                + "): "
                + message
                + " Dica: verifique permissões no SQL Server ou use ferramentas de catálogo/plano.",
            137 =>
                "Erro SQL (137): "
                + message
                + " Dica: possível problema de montagem de consulta; atualize o pacote McpSqlServer.",
            _ => $"Erro SQL ({exception.Number}): {message}",
        };
    }
}
