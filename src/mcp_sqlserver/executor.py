from mcp_sqlserver import config
from mcp_sqlserver.connection import connect
from mcp_sqlserver.formatters import format_result


def execute(sql: str, params: tuple = (), database: str | None = None) -> str:
    with connect(database) as conn:
        cursor = conn.execute(sql, *params)
        return format_result(cursor)


def execute_showplan(sql: str, database: str | None = None) -> str:
    with connect(database) as conn:
        conn.autocommit = True
        conn.execute("SET SHOWPLAN_XML ON")
        try:
            cursor = conn.execute(sql)
            if cursor.description is None:
                return "Plano de execução não retornado."
            rows = cursor.fetchmany(config.max_rows())
            parts = [str(row[0]) if row[0] is not None else "" for row in rows]
            if len(rows) >= config.max_rows():
                parts.append(f"... plano truncado em {config.max_rows()} fragmento(s) ...")
            return "\n".join(parts) if parts else "Plano de execução vazio."
        finally:
            conn.execute("SET SHOWPLAN_XML OFF")
