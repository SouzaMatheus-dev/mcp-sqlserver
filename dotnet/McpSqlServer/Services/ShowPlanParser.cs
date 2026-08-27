using System.Text;
using System.Xml.Linq;

namespace McpSqlServer.Services;

internal static class ShowPlanParser
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    public static string ExtractMissingIndexSuggestions(string xmlText)
    {
        if (string.IsNullOrWhiteSpace(xmlText))
        {
            return "Plano vazio.";
        }

        var suggestions = new List<string>();
        foreach (var document in LoadDocuments(xmlText))
        {
            foreach (var missing in document.Descendants(Ns + "MissingIndex"))
            {
                var schema = missing.Attribute("Schema")?.Value ?? string.Empty;
                var table = missing.Attribute("Table")?.Value ?? string.Empty;
                var impact = missing.Attribute("Impact")?.Value ?? string.Empty;

                var equality = ColumnsForUsage(missing, "EQUALITY");
                var inequality = ColumnsForUsage(missing, "INEQUALITY");
                var include = ColumnsForUsage(missing, "INCLUDE");

                suggestions.Add(
                    $"""
                    Tabela: [{schema}].[{table}] | Impacto estimado: {impact}
                      EQUALITY: {(equality.Count > 0 ? string.Join(", ", equality) : "-")}
                      INEQUALITY: {(inequality.Count > 0 ? string.Join(", ", inequality) : "-")}
                      INCLUDE: {(include.Count > 0 ? string.Join(", ", include) : "-")}
                    """);
            }
        }

        if (suggestions.Count == 0)
        {
            return "Nenhuma sugestão MissingIndex encontrada no plano.";
        }

        var output = new StringBuilder("=== Sugestões de índice (MissingIndex) ===");
        for (var index = 0; index < suggestions.Count; index++)
        {
            output.AppendLine().AppendLine().Append($"{index + 1}. ").Append(suggestions[index].Trim());
        }

        return output.ToString().TrimEnd();
    }

    private static IEnumerable<XDocument> LoadDocuments(string xmlText)
    {
        var fragments = System.Text.RegularExpressions.Regex.Matches(
            xmlText,
            "<ShowPlanXML[\\s\\S]*?</ShowPlanXML>");

        if (fragments.Count == 0)
        {
            yield return XDocument.Parse(xmlText);
            yield break;
        }

        foreach (System.Text.RegularExpressions.Match match in fragments)
        {
            yield return XDocument.Parse(match.Value);
        }
    }

    private static List<string> ColumnsForUsage(XElement missingIndex, string usage)
    {
        return missingIndex.Elements(Ns + "ColumnGroup")
            .Where(group => string.Equals(group.Attribute("Usage")?.Value, usage, StringComparison.Ordinal))
            .SelectMany(group => group.Elements(Ns + "Column"))
            .Select(column => column.Attribute("Name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();
    }
}
