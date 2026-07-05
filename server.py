"""Servidor MCP para SQL Server com autenticação integrada do Windows.

A conexão usa Trusted_Connection=yes, ou seja, o acesso ao banco é feito
com o usuário de rede (domínio) de quem está rodando o processo — sem
necessidade de usuário/senha de aplicação.

Configuração via variáveis de ambiente:
  MSSQL_SERVER    (obrigatória) - nome ou IP do servidor, ex: SRVSQL01\\PROD
  MSSQL_DATABASE  (opcional)    - banco padrão, ex: MeuBanco
  MSSQL_DRIVER    (opcional)    - driver ODBC, padrão: ODBC Driver 17 for SQL Server
  MSSQL_READONLY  (opcional)    - "false" para permitir escrita; padrão é somente leitura
"""

import os
import re

import pyodbc
from mcp.server.fastmcp import FastMCP

mcp = FastMCP("sqlserver")

MAX_LINHAS = 200


def _conectar(database: str | None = None) -> pyodbc.Connection:
    server = os.environ.get("MSSQL_SERVER")
    if not server:
        raise RuntimeError("Variável de ambiente MSSQL_SERVER não definida.")
    driver = os.environ.get("MSSQL_DRIVER", "ODBC Driver 17 for SQL Server")
    db = database or os.environ.get("MSSQL_DATABASE", "master")
    conn_str = (
        f"DRIVER={{{driver}}};"
        f"SERVER={server};"
        f"DATABASE={db};"
        "Trusted_Connection=yes;"
        "TrustServerCertificate=yes;"
    )
    return pyodbc.connect(conn_str, timeout=15)


def _somente_leitura() -> bool:
    return os.environ.get("MSSQL_READONLY", "true").lower() != "false"


def _formatar_resultado(cursor: pyodbc.Cursor) -> str:
    if cursor.description is None:
        return f"Comando executado. Linhas afetadas: {cursor.rowcount}"

    colunas = [c[0] for c in cursor.description]
    linhas = cursor.fetchmany(MAX_LINHAS)
    truncado = cursor.fetchone() is not None

    saida = [" | ".join(colunas)]
    saida.append("-" * len(saida[0]))
    for linha in linhas:
        saida.append(" | ".join("NULL" if v is None else str(v) for v in linha))
    if truncado:
        saida.append(f"... resultado truncado em {MAX_LINHAS} linhas ...")
    saida.append(f"({len(linhas)} linha(s) exibida(s))")
    return "\n".join(saida)


@mcp.tool()
def listar_bancos() -> str:
    """Lista os bancos de dados disponíveis no servidor."""
    with _conectar("master") as conn:
        cur = conn.execute(
            "SELECT name, state_desc FROM sys.databases ORDER BY name"
        )
        return _formatar_resultado(cur)


@mcp.tool()
def listar_tabelas(database: str = "", schema: str = "") -> str:
    """Lista tabelas e views de um banco. Opcionalmente filtra por schema."""
    sql = (
        "SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE "
        "FROM INFORMATION_SCHEMA.TABLES "
    )
    params: list[str] = []
    if schema:
        sql += "WHERE TABLE_SCHEMA = ? "
        params.append(schema)
    sql += "ORDER BY TABLE_SCHEMA, TABLE_NAME"
    with _conectar(database or None) as conn:
        cur = conn.execute(sql, *params)
        return _formatar_resultado(cur)


@mcp.tool()
def descrever_tabela(tabela: str, database: str = "", schema: str = "dbo") -> str:
    """Mostra as colunas, tipos e chaves de uma tabela."""
    sql = """
        SELECT
            c.COLUMN_NAME,
            c.DATA_TYPE,
            c.CHARACTER_MAXIMUM_LENGTH AS TAMANHO,
            c.IS_NULLABLE,
            c.COLUMN_DEFAULT,
            CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PK' ELSE '' END AS CHAVE
        FROM INFORMATION_SCHEMA.COLUMNS c
        LEFT JOIN (
            SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ) pk ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
            AND pk.TABLE_NAME = c.TABLE_NAME
            AND pk.COLUMN_NAME = c.COLUMN_NAME
        WHERE c.TABLE_NAME = ? AND c.TABLE_SCHEMA = ?
        ORDER BY c.ORDINAL_POSITION
    """
    with _conectar(database or None) as conn:
        cur = conn.execute(sql, tabela, schema)
        return _formatar_resultado(cur)


@mcp.tool()
def executar_consulta(sql: str, database: str = "") -> str:
    """Executa uma consulta SQL. Por padrão só permite SELECT (somente leitura)."""
    if _somente_leitura():
        primeiro_comando = re.sub(r"^\s*(--[^\n]*\n|/\*.*?\*/\s*)*", "", sql, flags=re.DOTALL).strip()
        if not re.match(r"^(SELECT|WITH)\b", primeiro_comando, re.IGNORECASE):
            return (
                "Bloqueado: o servidor está em modo somente leitura e aceita apenas "
                "SELECT/WITH. Defina MSSQL_READONLY=false para permitir escrita."
            )
    with _conectar(database or None) as conn:
        cur = conn.execute(sql)
        resultado = _formatar_resultado(cur)
        if not _somente_leitura():
            conn.commit()
        return resultado


@mcp.tool()
def usuario_conectado() -> str:
    """Mostra com qual usuário/login a conexão está chegando no SQL Server."""
    with _conectar() as conn:
        cur = conn.execute(
            "SELECT SUSER_SNAME() AS login_windows, "
            "USER_NAME() AS usuario_banco, "
            "DB_NAME() AS banco_atual, "
            "@@SERVERNAME AS servidor"
        )
        return _formatar_resultado(cur)


if __name__ == "__main__":
    mcp.run()
