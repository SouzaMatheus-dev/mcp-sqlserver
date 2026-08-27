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


def execute_with_statistics(sql: str, database: str | None = None) -> str:
    with connect(database) as conn:
        conn.autocommit = True
        cursor = conn.cursor()
        cursor.execute("SET STATISTICS IO, TIME ON")
        try:
            cursor.execute(sql)
            if cursor.description:
                while cursor.fetchmany(1000):
                    pass
            messages: list[str] = []
            for source in (cursor.messages, getattr(conn, "messages", None)):
                if not source:
                    continue
                for _level, msg in source:
                    text = str(msg).strip()
                    if text:
                        messages.append(text)
            if messages:
                return "\n".join(messages)
            return (
                "Consulta executada, mas STATISTICS IO/TIME não retornou mensagens. "
                "Verifique permissões no banco."
            )
        finally:
            cursor.execute("SET STATISTICS IO, TIME OFF")
