"""Parse de planos SHOWPLAN_XML do SQL Server."""

import re
import xml.etree.ElementTree as ET


def extrair_sugestoes_missing_index(xml_text: str) -> str:
    if not xml_text.strip():
        return "Plano vazio."

    suggestions: list[str] = []
    fragments = re.findall(r"<ShowPlanXML[\s\S]*?</ShowPlanXML>", xml_text)
    if not fragments:
        fragments = [xml_text]

    for fragment in fragments:
        try:
            root = ET.fromstring(fragment)
        except ET.ParseError:
            continue

        for node in root.iter():
            if not node.tag.endswith("MissingIndex"):
                continue

            schema = node.get("Schema", "")
            table = node.get("Table", "")
            impact = node.get("Impact", "")

            equality_cols: list[str] = []
            inequality_cols: list[str] = []
            include_cols: list[str] = []

            for group in node:
                if not group.tag.endswith("ColumnGroup"):
                    continue
                usage = group.get("Usage", "")
                cols = [
                    col.get("Name", "")
                    for col in group
                    if col.tag.endswith("Column") and col.get("Name")
                ]
                if usage == "EQUALITY":
                    equality_cols = cols
                elif usage == "INEQUALITY":
                    inequality_cols = cols
                elif usage == "INCLUDE":
                    include_cols = cols

            suggestions.append(
                f"Tabela: [{schema}].[{table}] | Impacto estimado: {impact}\n"
                f"  EQUALITY: {', '.join(equality_cols) or '-'}\n"
                f"  INEQUALITY: {', '.join(inequality_cols) or '-'}\n"
                f"  INCLUDE: {', '.join(include_cols) or '-'}"
            )

    if not suggestions:
        return "Nenhuma sugestão MissingIndex encontrada no plano."

    numbered = [f"{index}. {item}" for index, item in enumerate(suggestions, start=1)]
    return "=== Sugestões de índice (MissingIndex) ===\n\n" + "\n\n".join(numbered)
