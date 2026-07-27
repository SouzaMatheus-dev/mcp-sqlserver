"""Ferramentas MCP expostas ao cliente (Gemini, Cursor, Claude etc.)."""

from mcp.server.fastmcp import FastMCP

from mcp_sqlserver import config
from mcp_sqlserver.connection import connect
from mcp_sqlserver.formatters import format_result
from mcp_sqlserver.readonly import validar_consulta_leitura

mcp = FastMCP(
    "sqlserver",
    instructions=(
        "Especialista em SQL Server corporativo (somente leitura). "
        "Antes de consultar dados: 1) listar_chaves_estrangeiras para entender JOINs, "
        "2) obter_documentacao_objeto para MS_Description, "
        "3) buscar_coluna/buscar_objeto para descobrir nomes. "
        "Use o parâmetro database para trocar de banco no mesmo servidor."
    ),
)


def _like_pattern(termo: str) -> str:
    escaped = termo.replace("[", "[[]").replace("%", "[%]").replace("_", "[_]")
    return f"%{escaped}%"


def _executar(sql: str, params: tuple = (), database: str | None = None) -> str:
    with connect(database) as conn:
        cursor = conn.execute(sql, *params)
        return format_result(cursor)


@mcp.tool()
def usuario_conectado() -> str:
    """Mostra o login Windows, usuário de banco, banco atual e servidor."""
    return _executar(
        "SELECT SUSER_SNAME() AS login_windows, "
        "USER_NAME() AS usuario_banco, "
        "DB_NAME() AS banco_atual, "
        "@@SERVERNAME AS servidor, "
        "CASE WHEN ? = 1 THEN 'somente_leitura' ELSE 'leitura_escrita' END AS modo_mcp",
        (1 if config.readonly() else 0,),
    )


@mcp.tool()
def listar_bancos() -> str:
    """Lista bancos de dados visíveis para o usuário de rede conectado."""
    return _executar(
        "SELECT name, state_desc, recovery_model_desc "
        "FROM sys.databases ORDER BY name",
        database="master",
    )


@mcp.tool()
def listar_tabelas(database: str = "", schema: str = "") -> str:
    """Lista apenas tabelas base (não inclui views)."""
    sql = (
        "SELECT TABLE_SCHEMA, TABLE_NAME "
        "FROM INFORMATION_SCHEMA.TABLES "
        "WHERE TABLE_TYPE = 'BASE TABLE' "
    )
    params: list[str] = []
    if schema:
        sql += "AND TABLE_SCHEMA = ? "
        params.append(schema)
    sql += "ORDER BY TABLE_SCHEMA, TABLE_NAME"
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def listar_views(database: str = "", schema: str = "") -> str:
    """Lista views disponíveis para leitura no banco."""
    sql = (
        "SELECT TABLE_SCHEMA, TABLE_NAME "
        "FROM INFORMATION_SCHEMA.VIEWS "
    )
    params: list[str] = []
    if schema:
        sql += "WHERE TABLE_SCHEMA = ? "
        params.append(schema)
    sql += "ORDER BY TABLE_SCHEMA, TABLE_NAME"
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def listar_procedures(database: str = "", schema: str = "") -> str:
    """Lista stored procedures e functions visíveis (somente metadados)."""
    sql = (
        "SELECT "
        "SCHEMA_NAME(o.schema_id) AS schema_name, "
        "o.name AS object_name, "
        "o.type_desc AS tipo, "
        "o.create_date, "
        "o.modify_date "
        "FROM sys.objects o "
        "WHERE o.type IN ('P', 'PC', 'FN', 'IF', 'TF') "
    )
    params: list[str] = []
    if schema:
        sql += "AND SCHEMA_NAME(o.schema_id) = ? "
        params.append(schema)
    sql += "ORDER BY schema_name, object_name"
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def descrever_tabela(tabela: str, database: str = "", schema: str = "dbo") -> str:
    """Mostra colunas, tipos, nulabilidade e chave primária de uma tabela."""
    sql = """
        SELECT
            c.COLUMN_NAME,
            c.DATA_TYPE,
            c.CHARACTER_MAXIMUM_LENGTH AS tamanho,
            c.IS_NULLABLE,
            c.COLUMN_DEFAULT,
            CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PK' ELSE '' END AS chave
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
    return _executar(sql, (tabela, schema), database or None)


@mcp.tool()
def descrever_view(view: str, database: str = "", schema: str = "dbo") -> str:
    """Mostra colunas e tipos de uma view."""
    sql = """
        SELECT
            c.COLUMN_NAME,
            c.DATA_TYPE,
            c.CHARACTER_MAXIMUM_LENGTH AS tamanho,
            c.IS_NULLABLE
        FROM INFORMATION_SCHEMA.COLUMNS c
        JOIN INFORMATION_SCHEMA.VIEWS v
            ON v.TABLE_SCHEMA = c.TABLE_SCHEMA
            AND v.TABLE_NAME = c.TABLE_NAME
        WHERE c.TABLE_NAME = ? AND c.TABLE_SCHEMA = ?
        ORDER BY c.ORDINAL_POSITION
    """
    return _executar(sql, (view, schema), database or None)


@mcp.tool()
def descrever_procedure(procedure: str, database: str = "", schema: str = "dbo") -> str:
    """Mostra parâmetros, tipos e direção de entrada/saída de uma procedure ou function."""
    sql = """
        SELECT
            p.name AS parametro,
            TYPE_NAME(p.user_type_id) AS tipo,
            p.max_length,
            p.is_output,
            p.has_default_value,
            p.default_value
        FROM sys.objects o
        JOIN sys.parameters p ON p.object_id = o.object_id
        WHERE o.name = ?
          AND SCHEMA_NAME(o.schema_id) = ?
          AND o.type IN ('P', 'PC', 'FN', 'IF', 'TF')
        ORDER BY p.parameter_id
    """
    return _executar(sql, (procedure, schema), database or None)


@mcp.tool()
def obter_definicao_sql(
    objeto: str,
    database: str = "",
    schema: str = "dbo",
    tipo: str = "",
) -> str:
    """Retorna o script SQL de uma view, procedure ou function (somente leitura de metadados)."""
    sql = """
        SELECT
            SCHEMA_NAME(o.schema_id) AS schema_name,
            o.name AS object_name,
            o.type_desc AS tipo,
            m.definition AS definicao_sql
        FROM sys.sql_modules m
        JOIN sys.objects o ON o.object_id = m.object_id
        WHERE o.name = ?
          AND SCHEMA_NAME(o.schema_id) = ?
          AND o.type IN ('V', 'P', 'PC', 'FN', 'IF', 'TF')
    """
    params: list[str] = [objeto, schema]
    if tipo:
        sql += " AND o.type_desc = ? "
        params.append(tipo.upper())
    sql += " ORDER BY o.type_desc"
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def consultar_view(
    view: str,
    database: str = "",
    schema: str = "dbo",
    top: int = 100,
) -> str:
    """Consulta uma view com SELECT TOP (somente leitura)."""
    if top < 1 or top > config.max_rows():
        return f"Bloqueado: TOP deve estar entre 1 e {config.max_rows()}."
    if not schema.replace("_", "").isalnum() or not view.replace("_", "").isalnum():
        return "Bloqueado: schema e view devem conter apenas letras, números e underscore."

    sql = f"SELECT TOP ({top}) * FROM [{schema}].[{view}]"
    erro = validar_consulta_leitura(sql)
    if erro:
        return erro
    return _executar(sql, database=database or None)


@mcp.tool()
def executar_consulta(sql: str, database: str = "") -> str:
    """Executa SELECT/WITH validado. Bloqueia DDL, DML e EXEC em modo corporativo."""
    if config.readonly():
        erro = validar_consulta_leitura(sql)
        if erro:
            return erro
    return _executar(sql, database=database or None)


@mcp.tool()
def listar_chaves_estrangeiras(
    database: str = "",
    schema: str = "",
    tabela: str = "",
) -> str:
    """Lista foreign keys do banco. Opcionalmente filtra por schema ou tabela."""
    sql = """
        SELECT
            fk.name AS constraint_name,
            SCHEMA_NAME(tp.schema_id) AS tabela_origem_schema,
            tp.name AS tabela_origem,
            cp.name AS coluna_origem,
            SCHEMA_NAME(tr.schema_id) AS tabela_destino_schema,
            tr.name AS tabela_destino,
            cr.name AS coluna_destino
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
        INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
        INNER JOIN sys.columns cp
            ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
        INNER JOIN sys.columns cr
            ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
        WHERE 1 = 1
    """
    params: list[str] = []
    if schema:
        sql += " AND SCHEMA_NAME(tp.schema_id) = ? "
        params.append(schema)
    if tabela:
        sql += " AND tp.name = ? "
        params.append(tabela)
    sql += " ORDER BY tabela_origem_schema, tabela_origem, constraint_name"
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def buscar_coluna(termo: str, database: str = "") -> str:
    """Busca colunas pelo nome (LIKE). Ex: 'Cliente' encontra ClienteId, NomeCliente etc."""
    if not termo.strip():
        return "Informe um termo de busca para a coluna."
    sql = """
        SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE COLUMN_NAME LIKE ?
        ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
    """
    return _executar(sql, (_like_pattern(termo.strip()),), database or None)


@mcp.tool()
def buscar_objeto(termo: str, database: str = "") -> str:
    """Busca tabelas, views e procedures pelo nome (LIKE)."""
    if not termo.strip():
        return "Informe um termo de busca para o objeto."
    sql = """
        SELECT
            SCHEMA_NAME(o.schema_id) AS schema_name,
            o.name AS object_name,
            o.type_desc AS tipo,
            o.create_date,
            o.modify_date
        FROM sys.objects o
        WHERE o.type IN ('U', 'V', 'P', 'PC', 'FN', 'IF', 'TF')
          AND o.name LIKE ?
        ORDER BY schema_name, object_name
    """
    return _executar(sql, (_like_pattern(termo.strip()),), database or None)


@mcp.tool()
def obter_documentacao_objeto(
    objeto: str,
    database: str = "",
    schema: str = "dbo",
) -> str:
    """Retorna MS_Description (documentação) de tabela/view/procedure e suas colunas."""
    sql_objeto = """
        SELECT
            SCHEMA_NAME(o.schema_id) AS schema_name,
            o.name AS object_name,
            o.type_desc AS tipo,
            'objeto' AS nivel,
            CAST(ep.value AS NVARCHAR(MAX)) AS descricao
        FROM sys.extended_properties ep
        INNER JOIN sys.objects o ON ep.major_id = o.object_id AND ep.minor_id = 0
        WHERE ep.name = 'MS_Description'
          AND o.name = ?
          AND SCHEMA_NAME(o.schema_id) = ?
        UNION ALL
        SELECT
            SCHEMA_NAME(o.schema_id) AS schema_name,
            o.name AS object_name,
            o.type_desc AS tipo,
            c.name AS nivel,
            CAST(ep.value AS NVARCHAR(MAX)) AS descricao
        FROM sys.extended_properties ep
        INNER JOIN sys.columns c ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
        INNER JOIN sys.objects o ON c.object_id = o.object_id
        WHERE ep.name = 'MS_Description'
          AND o.name = ?
          AND SCHEMA_NAME(o.schema_id) = ?
        ORDER BY nivel
    """
    params = (objeto, schema, objeto, schema)
    return _executar(sql_objeto, params, database or None)


def run() -> None:
    mcp.run()
