import os


def server() -> str:
    value = os.environ.get("MSSQL_SERVER", "").strip()
    if not value:
        raise RuntimeError("Variável de ambiente MSSQL_SERVER não definida.")
    return value


def database(default: str = "master") -> str:
    return os.environ.get("MSSQL_DATABASE", default).strip() or default


def driver() -> str:
    return os.environ.get("MSSQL_DRIVER", "ODBC Driver 17 for SQL Server").strip()


def connection_timeout() -> int:
    return int(os.environ.get("MSSQL_CONNECTION_TIMEOUT", "15"))


def max_rows() -> int:
    return int(os.environ.get("MSSQL_MAX_ROWS", "200"))


def readonly() -> bool:
    return os.environ.get("MSSQL_READONLY", "true").lower() != "false"


def application_intent_readonly() -> bool:
    return os.environ.get("MSSQL_APPLICATION_INTENT_READONLY", "true").lower() != "false"


def performance_dmvs_enabled() -> bool:
    return os.environ.get("MSSQL_ENABLE_PERFORMANCE_DMVS", "false").lower() == "true"

