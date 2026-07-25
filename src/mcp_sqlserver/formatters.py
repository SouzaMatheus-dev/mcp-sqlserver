import pyodbc

from mcp_sqlserver import config


def format_result(cursor: pyodbc.Cursor) -> str:
    if cursor.description is None:
        return f"Comando executado. Linhas afetadas: {cursor.rowcount}"

    max_rows = config.max_rows()
    columns = [column[0] for column in cursor.description]
    rows = cursor.fetchmany(max_rows)
    truncated = cursor.fetchone() is not None

    header = " | ".join(columns)
    output = [header, "-" * len(header)]
    for row in rows:
        output.append(" | ".join("NULL" if value is None else str(value) for value in row))
    if truncated:
        output.append(f"... resultado truncado em {max_rows} linhas ...")
    output.append(f"({len(rows)} linha(s) exibida(s))")
    return "\n".join(output)
