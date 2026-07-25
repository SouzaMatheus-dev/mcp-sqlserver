import pyodbc

from mcp_sqlserver import config


def connect(database: str | None = None) -> pyodbc.Connection:
    db = database or config.database()
    parts = [
        f"DRIVER={{{config.driver()}}}",
        f"SERVER={config.server()}",
        f"DATABASE={db}",
        "Trusted_Connection=yes",
        "TrustServerCertificate=yes",
    ]
    if config.readonly() and config.application_intent_readonly():
        parts.append("ApplicationIntent=ReadOnly")
    conn = pyodbc.connect(";".join(parts) + ";", timeout=config.connection_timeout())
    if config.readonly():
        conn.autocommit = True
    return conn
