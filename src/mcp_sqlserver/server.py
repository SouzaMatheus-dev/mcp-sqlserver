"""Ferramentas MCP expostas ao cliente (Gemini, Cursor, Claude etc.)."""

from mcp.server.fastmcp import FastMCP

from mcp_sqlserver import config
from mcp_sqlserver.connection import connect
from mcp_sqlserver.executor import execute as _executar
from mcp_sqlserver.readonly import validar_consulta_leitura
from mcp_sqlserver.security import identificador_seguro

mcp = FastMCP(
    "sqlserver",
    instructions=(
        "Especialista em SQL Server corporativo (somente leitura). "
        "Antes de consultar dados: 1) listar_chaves_estrangeiras e listar_indices para entender JOINs, "
        "2) listar_dependencias para impacto entre objetos, "
        "3) resumir_banco para visão geral, "
        "4) obter_documentacao_objeto para MS_Description, "
        "5) buscar_coluna/buscar_objeto/buscar_texto_sql para descobrir nomes e lógica, "
        "6) amostrar_tabela e perfil_coluna para entender os dados. "
        "Use o parâmetro database para trocar de banco no mesmo servidor."
    ),
)


def _like_pattern(termo: str) -> str:
    escaped = termo.replace("[", "[[]").replace("%", "[%]").replace("_", "[_]")
    return f"%{escaped}%"


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


@mcp.tool()
def listar_indices(
    database: str = "",
    schema: str = "",
    tabela: str = "",
) -> str:
    """Lista índices (clustered, nonclustered, unique, PK). Filtra por schema/tabela."""
    sql = """
        SELECT
            SCHEMA_NAME(t.schema_id) AS schema_name,
            t.name AS tabela,
            i.name AS indice,
            i.type_desc AS tipo_indice,
            i.is_unique,
            i.is_primary_key,
            STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS colunas
        FROM sys.indexes i
        INNER JOIN sys.tables t ON i.object_id = t.object_id
        INNER JOIN sys.index_columns ic
            ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns c
            ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE i.type > 0
    """
    params: list[str] = []
    if schema:
        sql += " AND SCHEMA_NAME(t.schema_id) = ? "
        params.append(schema)
    if tabela:
        sql += " AND t.name = ? "
        params.append(tabela)
    sql += """
        GROUP BY SCHEMA_NAME(t.schema_id), t.name, i.name, i.type_desc, i.is_unique, i.is_primary_key
        ORDER BY schema_name, tabela, indice
    """
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def listar_dependencias(
    objeto: str,
    database: str = "",
    schema: str = "dbo",
    direcao: str = "ambos",
) -> str:
    """Lista dependências SQL de/para um objeto (views, procs, tabelas). direcao: ambos, referencia, referenciado_por."""
    direcao_norm = direcao.strip().lower()
    if direcao_norm not in ("ambos", "referencia", "referenciado_por"):
        return "direcao deve ser: ambos, referencia ou referenciado_por."

    sql = """
        SELECT
            SCHEMA_NAME(o_ref.schema_id) AS referenciador_schema,
            o_ref.name AS referenciador,
            o_ref.type_desc AS tipo_referenciador,
            SCHEMA_NAME(o_refd.schema_id) AS referenciado_schema,
            o_refd.name AS referenciado,
            o_refd.type_desc AS tipo_referenciado
        FROM sys.sql_expression_dependencies d
        INNER JOIN sys.objects o_ref ON d.referencing_id = o_ref.object_id
        INNER JOIN sys.objects o_refd ON d.referenced_id = o_refd.object_id
        WHERE 1 = 1
    """
    params: list[str] = []
    if direcao_norm in ("ambos", "referencia"):
        sql += " AND o_ref.name = ? AND SCHEMA_NAME(o_ref.schema_id) = ? "
        params.extend([objeto, schema])
    elif direcao_norm == "referenciado_por":
        sql += " AND o_refd.name = ? AND SCHEMA_NAME(o_refd.schema_id) = ? "
        params.extend([objeto, schema])

    if direcao_norm == "ambos":
        sql += """
            OR (o_refd.name = ? AND SCHEMA_NAME(o_refd.schema_id) = ?)
        """
        params.extend([objeto, schema])

    sql += " ORDER BY referenciador_schema, referenciador, referenciado_schema, referenciado"
    return _executar(sql, tuple(params), database or None)


@mcp.tool()
def resumir_banco(database: str = "") -> str:
    """Visão geral do banco: contagens por tipo de objeto e TOP 20 maiores tabelas."""
    sql_contagens = """
        SELECT o.type_desc AS categoria, COUNT(*) AS quantidade
        FROM sys.objects o
        WHERE o.type IN ('U', 'V', 'P', 'PC', 'FN', 'IF', 'TF', 'TR')
        GROUP BY o.type_desc
        ORDER BY quantidade DESC
    """
    sql_maiores = """
        SELECT TOP 20
            SCHEMA_NAME(t.schema_id) AS schema_name,
            t.name AS tabela,
            SUM(p.rows) AS linhas_aprox,
            CAST(SUM(a.total_pages) * 8.0 / 1024 AS DECIMAL(18, 2)) AS tamanho_mb
        FROM sys.tables t
        INNER JOIN sys.indexes i ON t.object_id = i.object_id
        INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
        INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
        WHERE i.index_id IN (0, 1)
        GROUP BY SCHEMA_NAME(t.schema_id), t.name
        ORDER BY tamanho_mb DESC
    """
    db = database or None
    contagens = _executar(sql_contagens, database=db)
    maiores = _executar(sql_maiores, database=db)
    return (
        "=== Contagens por tipo de objeto ===\n"
        f"{contagens}\n\n"
        "=== Maiores tabelas (TOP 20 por tamanho) ===\n"
        f"{maiores}"
    )


_TIPOS_COMPARAVEIS = frozenset({
    "int", "bigint", "smallint", "tinyint", "decimal", "numeric", "float", "real",
    "money", "smallmoney", "date", "datetime", "datetime2", "smalldatetime", "time",
})


def _validar_identificadores(*nomes: str) -> str | None:
    for nome in nomes:
        if identificador_seguro(nome) is None:
            return "Bloqueado: identificadores devem conter apenas letras, números e underscore."
    return None


@mcp.tool()
def amostrar_tabela(tabela: str, database: str = "", schema: str = "dbo", top: int = 20) -> str:
    """Amostra linhas de uma tabela (SELECT TOP N)."""
    if top < 1 or top > config.max_rows():
        return f"Bloqueado: top deve estar entre 1 e {config.max_rows()}."
    erro_id = _validar_identificadores(schema, tabela)
    if erro_id:
        return erro_id
    sql = f"SELECT TOP ({top}) * FROM [{schema}].[{tabela}]"
    erro = validar_consulta_leitura(sql)
    if erro:
        return erro
    return _executar(sql, database=database or None)


@mcp.tool()
def perfil_coluna(tabela: str, coluna: str, database: str = "", schema: str = "dbo") -> str:
    """Perfil estatístico de uma coluna: nulos, distintos, min/max quando aplicável e amostras."""
    erro_id = _validar_identificadores(schema, tabela, coluna)
    if erro_id:
        return erro_id

    meta_sql = """
        SELECT DATA_TYPE
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ? AND COLUMN_NAME = ?
    """
    db = database or None
    meta = _executar(meta_sql, (schema, tabela, coluna), db)
    if "0 linha(s)" in meta:
        return f"Coluna [{schema}].[{tabela}].[{coluna}] não encontrada."

    data_type = ""
    for line in meta.splitlines()[2:]:
        if line.strip() and not line.startswith("-"):
            data_type = line.strip().split("|")[0].strip().lower()
            break

    stats_sql = f"""
        SELECT
            COUNT(*) AS total_linhas,
            SUM(CASE WHEN [{coluna}] IS NULL THEN 1 ELSE 0 END) AS nulos,
            COUNT(DISTINCT [{coluna}]) AS valores_distintos
        FROM [{schema}].[{tabela}]
    """
    stats = _executar(stats_sql, database=db)

    extras: list[str] = []
    if data_type in _TIPOS_COMPARAVEIS:
        minmax_sql = f"""
            SELECT MIN([{coluna}]) AS minimo, MAX([{coluna}]) AS maximo
            FROM [{schema}].[{tabela}]
        """
        extras.append("=== Min / Max ===")
        extras.append(_executar(minmax_sql, database=db))

    amostra_sql = f"""
        SELECT TOP 5 [{coluna}] AS valor, COUNT(*) AS ocorrencias
        FROM [{schema}].[{tabela}]
        WHERE [{coluna}] IS NOT NULL
        GROUP BY [{coluna}]
        ORDER BY ocorrencias DESC
    """
    extras.append("=== Valores mais frequentes (TOP 5) ===")
    extras.append(_executar(amostra_sql, database=db))

    return (
        f"=== Perfil de [{schema}].[{tabela}].[{coluna}] (tipo: {data_type or 'desconhecido'}) ===\n"
        f"{stats}\n\n"
        + "\n\n".join(extras)
    )


@mcp.tool()
def buscar_texto_sql(termo: str, database: str = "", schema: str = "") -> str:
    """Busca texto dentro de views, procedures e functions (sys.sql_modules)."""
    if not termo.strip():
        return "Informe um termo de busca."
    sql = """
        SELECT
            SCHEMA_NAME(o.schema_id) AS schema_name,
            o.name AS object_name,
            o.type_desc AS tipo,
            LEFT(m.definition, 500) AS trecho
        FROM sys.sql_modules m
        INNER JOIN sys.objects o ON o.object_id = m.object_id
        WHERE m.definition LIKE ?
          AND o.type IN ('V', 'P', 'PC', 'FN', 'IF', 'TF')
    """
    params: list[str] = [_like_pattern(termo)]
    if schema:
        sql += " AND SCHEMA_NAME(o.schema_id) = ?"
        params.append(schema)
    sql += " ORDER BY schema_name, object_name"
    return _executar(sql, tuple(params), database or None)


def run() -> None:
    mcp.run()
