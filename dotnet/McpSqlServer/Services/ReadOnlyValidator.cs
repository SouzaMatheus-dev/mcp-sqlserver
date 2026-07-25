using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace McpSqlServer.Services;

public static partial class ReadOnlyValidator
{
    private static readonly string[] BlockedKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "MERGE", "TRUNCATE", "DROP", "CREATE", "ALTER", "RENAME",
        "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "BULK", "OPENROWSET", "OPENDATASOURCE",
        "OPENQUERY", "OPENXML", "SHUTDOWN", "DBCC", "KILL", "RECONFIGURE", "WAITFOR",
    ];

    public static string? Validate(string sql)
    {
        var normalized = Normalize(sql);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Consulta vazia não é permitida.";
        }

        if (!AllowedStart().IsMatch(normalized))
        {
            return "Bloqueado: em modo corporativo somente leitura só são permitidas consultas que começam com SELECT ou WITH.";
        }

        var upper = normalized.ToUpperInvariant();
        foreach (var keyword in BlockedKeywords)
        {
            if (KeywordPattern(keyword).IsMatch(upper))
            {
                return $"Bloqueado: palavra-chave não permitida em modo leitura: {keyword}.";
            }
        }

        if (SpExecuteSql().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (sp_executesql).";
        }

        if (XpCmd().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (xp_*).";
        }

        if (SelectInto().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (INTO).";
        }

        if (MultipleStatements().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (;).";
        }

        return null;
    }

    private static string Normalize(string sql)
    {
        var withoutBlockComments = BlockComment().Replace(sql, " ");
        var withoutLineComments = LineComment().Replace(withoutBlockComments, " ");
        return Whitespace().Replace(withoutLineComments, " ").Trim();
    }

    [GeneratedRegex(@"^(SELECT|WITH)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AllowedStart();

    [GeneratedRegex(@"\bINTO\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectInto();

    [GeneratedRegex(@";\s*\S", RegexOptions.CultureInvariant)]
    private static partial Regex MultipleStatements();

    [GeneratedRegex(@"\bsp_executesql\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpExecuteSql();

    [GeneratedRegex(@"\bxp_\w+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex XpCmd();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"--[^\n\r]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineComment();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    private static Regex KeywordPattern(string keyword) =>
        new($@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
