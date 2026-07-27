using System.ComponentModel;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using McpSqlServer.Services;

namespace McpSqlServer.Tools;

[McpServerToolType]
public sealed class SqlTools(McpConfig config, SqlExecutor executor)
{
    [McpServerTool, Description("Mostra o login Windows, usuário de banco, banco atual e servidor.")]
    public string UsuarioConectado()
    {
        var modo = config.ReadOnly ? "somente_leitura" : "leitura_escrita";
        return executor.Execute(
            "SELECT SUSER_SNAME() AS login_windows, USER_NAME() AS usuario_banco, " +
            "DB_NAME() AS banco_atual, @@SERVERNAME AS servidor, @modo AS modo_mcp",
            parameters: [new SqlParameter("@modo", modo)]);
    }

    [McpServerTool, Description("Lista bancos de dados visíveis para o usuário de rede conectado.")]
    public string ListarBancos()
    {
        return executor.Execute(
            "SELECT name, state_desc, recovery_model_desc FROM sys.databases ORDER BY name",
            database: "master");
    }

    [McpServerTool, Description("Lista apenas tabelas base (não inclui views).")]
    public string ListarTabelas(string database = "", string schema = "")
    {
        var sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            """;
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND TABLE_SCHEMA = @schema";
            parameters.Add(new SqlParameter("@schema", schema));
        }
        sql += " ORDER BY TABLE_SCHEMA, TABLE_NAME";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Lista views disponíveis para leitura no banco.")]
    public string ListarViews(string database = "", string schema = "")
    {
        var sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.VIEWS
            """;
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " WHERE TABLE_SCHEMA = @schema";
            parameters.Add(new SqlParameter("@schema", schema));
        }
        sql += " ORDER BY TABLE_SCHEMA, TABLE_NAME";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Lista stored procedures e functions visíveis (somente metadados).")]
    public string ListarProcedures(string database = "", string schema = "")
    {
        var sql = """
            SELECT
                SCHEMA_NAME(o.schema_id) AS schema_name,
                o.name AS object_name,
                o.type_desc AS tipo,
                o.create_date,
                o.modify_date
            FROM sys.objects o
            WHERE o.type IN ('P', 'PC', 'FN', 'IF', 'TF')
            """;
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND SCHEMA_NAME(o.schema_id) = @schema";
            parameters.Add(new SqlParameter("@schema", schema));
        }
        sql += " ORDER BY schema_name, object_name";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Mostra colunas, tipos, nulabilidade e chave primária de uma tabela.")]
    public string DescreverTabela(string tabela, string database = "", string schema = "dbo")
    {
        const string sql = """
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
            WHERE c.TABLE_NAME = @tabela AND c.TABLE_SCHEMA = @schema
            ORDER BY c.ORDINAL_POSITION
            """;
        return executor.Execute(
            sql,
            string.IsNullOrWhiteSpace(database) ? null : database,
            new SqlParameter("@tabela", tabela),
            new SqlParameter("@schema", schema));
    }

    [McpServerTool, Description("Mostra colunas e tipos de uma view.")]
    public string DescreverView(string view, string database = "", string schema = "dbo")
    {
        const string sql = """
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH AS tamanho,
                c.IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS c
            JOIN INFORMATION_SCHEMA.VIEWS v
                ON v.TABLE_SCHEMA = c.TABLE_SCHEMA
                AND v.TABLE_NAME = c.TABLE_NAME
            WHERE c.TABLE_NAME = @view AND c.TABLE_SCHEMA = @schema
            ORDER BY c.ORDINAL_POSITION
            """;
        return executor.Execute(
            sql,
            string.IsNullOrWhiteSpace(database) ? null : database,
            new SqlParameter("@view", view),
            new SqlParameter("@schema", schema));
    }

    [McpServerTool, Description("Mostra parâmetros, tipos e direção de uma procedure ou function.")]
    public string DescreverProcedure(string procedure, string database = "", string schema = "dbo")
    {
        const string sql = """
            SELECT
                p.name AS parametro,
                TYPE_NAME(p.user_type_id) AS tipo,
                p.max_length,
                p.is_output,
                p.has_default_value,
                p.default_value
            FROM sys.objects o
            JOIN sys.parameters p ON p.object_id = o.object_id
            WHERE o.name = @procedure
              AND SCHEMA_NAME(o.schema_id) = @schema
              AND o.type IN ('P', 'PC', 'FN', 'IF', 'TF')
            ORDER BY p.parameter_id
            """;
        return executor.Execute(
            sql,
            string.IsNullOrWhiteSpace(database) ? null : database,
            new SqlParameter("@procedure", procedure),
            new SqlParameter("@schema", schema));
    }

    [McpServerTool, Description("Retorna o script SQL de uma view, procedure ou function.")]
    public string ObterDefinicaoSql(string objeto, string database = "", string schema = "dbo", string tipo = "")
    {
        var sql = """
            SELECT
                SCHEMA_NAME(o.schema_id) AS schema_name,
                o.name AS object_name,
                o.type_desc AS tipo,
                m.definition AS definicao_sql
            FROM sys.sql_modules m
            JOIN sys.objects o ON o.object_id = m.object_id
            WHERE o.name = @objeto
              AND SCHEMA_NAME(o.schema_id) = @schema
              AND o.type IN ('V', 'P', 'PC', 'FN', 'IF', 'TF')
            """;
        var parameters = new List<SqlParameter>
        {
            new("@objeto", objeto),
            new("@schema", schema),
        };
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            sql += " AND o.type_desc = @tipo";
            parameters.Add(new SqlParameter("@tipo", tipo.ToUpperInvariant()));
        }
        sql += " ORDER BY o.type_desc";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Consulta uma view com SELECT TOP (somente leitura).")]
    public string ConsultarView(string view, string database = "", string schema = "dbo", int top = 100)
    {
        if (top < 1 || top > config.MaxRows)
        {
            return $"Bloqueado: TOP deve estar entre 1 e {config.MaxRows}.";
        }

        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(view))
        {
            return "Bloqueado: schema e view devem conter apenas letras, números e underscore.";
        }

        var sql = $"SELECT TOP ({top}) * FROM [{schema}].[{view}]";
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database);
    }

    [McpServerTool, Description("Executa SELECT/WITH validado. Bloqueia DDL, DML e EXEC em modo corporativo.")]
    public string ExecutarConsulta(string sql, string database = "")
    {
        if (config.ReadOnly)
        {
            var error = ReadOnlyValidator.Validate(sql);
            if (error is not null)
            {
                return error;
            }
        }

        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database);
    }

    [McpServerTool, Description("Lista foreign keys do banco. Opcionalmente filtra por schema ou tabela.")]
    public string ListarChavesEstrangeiras(string database = "", string schema = "", string tabela = "")
    {
        var sql = """
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
            """;
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND SCHEMA_NAME(tp.schema_id) = @schema";
            parameters.Add(new SqlParameter("@schema", schema));
        }
        if (!string.IsNullOrWhiteSpace(tabela))
        {
            sql += " AND tp.name = @tabela";
            parameters.Add(new SqlParameter("@tabela", tabela));
        }
        sql += " ORDER BY tabela_origem_schema, tabela_origem, constraint_name";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Busca colunas pelo nome (LIKE). Ex: 'Cliente' encontra ClienteId, NomeCliente etc.")]
    public string BuscarColuna(string termo, string database = "")
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return "Informe um termo de busca para a coluna.";
        }

        const string sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE COLUMN_NAME LIKE @termo
            ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
            """;
        return executor.Execute(
            sql,
            string.IsNullOrWhiteSpace(database) ? null : database,
            new SqlParameter("@termo", LikePattern(termo)));
    }

    [McpServerTool, Description("Busca tabelas, views e procedures pelo nome (LIKE).")]
    public string BuscarObjeto(string termo, string database = "")
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return "Informe um termo de busca para o objeto.";
        }

        const string sql = """
            SELECT
                SCHEMA_NAME(o.schema_id) AS schema_name,
                o.name AS object_name,
                o.type_desc AS tipo,
                o.create_date,
                o.modify_date
            FROM sys.objects o
            WHERE o.type IN ('U', 'V', 'P', 'PC', 'FN', 'IF', 'TF')
              AND o.name LIKE @termo
            ORDER BY schema_name, object_name
            """;
        return executor.Execute(
            sql,
            string.IsNullOrWhiteSpace(database) ? null : database,
            new SqlParameter("@termo", LikePattern(termo)));
    }

    [McpServerTool, Description("Retorna MS_Description (documentação) de tabela/view/procedure e suas colunas.")]
    public string ObterDocumentacaoObjeto(string objeto, string database = "", string schema = "dbo")
    {
        const string sql = """
            SELECT
                SCHEMA_NAME(o.schema_id) AS schema_name,
                o.name AS object_name,
                o.type_desc AS tipo,
                'objeto' AS nivel,
                CAST(ep.value AS NVARCHAR(MAX)) AS descricao
            FROM sys.extended_properties ep
            INNER JOIN sys.objects o ON ep.major_id = o.object_id AND ep.minor_id = 0
            WHERE ep.name = 'MS_Description'
              AND o.name = @objeto
              AND SCHEMA_NAME(o.schema_id) = @schema
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
              AND o.name = @objeto
              AND SCHEMA_NAME(o.schema_id) = @schema
            ORDER BY nivel
            """;
        return executor.Execute(
            sql,
            string.IsNullOrWhiteSpace(database) ? null : database,
            new SqlParameter("@objeto", objeto),
            new SqlParameter("@schema", schema));
    }

    [McpServerTool, Description("Lista índices (clustered, nonclustered, unique, PK). Filtra por schema/tabela.")]
    public string ListarIndices(string database = "", string schema = "", string tabela = "")
    {
        var sql = """
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
            """;
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND SCHEMA_NAME(t.schema_id) = @schema";
            parameters.Add(new SqlParameter("@schema", schema));
        }
        if (!string.IsNullOrWhiteSpace(tabela))
        {
            sql += " AND t.name = @tabela";
            parameters.Add(new SqlParameter("@tabela", tabela));
        }
        sql += """
            GROUP BY SCHEMA_NAME(t.schema_id), t.name, i.name, i.type_desc, i.is_unique, i.is_primary_key
            ORDER BY schema_name, tabela, indice
            """;
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Lista dependências SQL de/para um objeto. direcao: ambos, referencia, referenciado_por.")]
    public string ListarDependencias(
        string objeto,
        string database = "",
        string schema = "dbo",
        string direcao = "ambos")
    {
        var direcaoNorm = direcao.Trim().ToLowerInvariant();
        if (direcaoNorm is not ("ambos" or "referencia" or "referenciado_por"))
        {
            return "direcao deve ser: ambos, referencia ou referenciado_por.";
        }

        var sql = """
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
            """;
        var parameters = new List<SqlParameter>();
        if (direcaoNorm is "ambos" or "referencia")
        {
            sql += " AND o_ref.name = @objeto AND SCHEMA_NAME(o_ref.schema_id) = @schema";
            parameters.Add(new SqlParameter("@objeto", objeto));
            parameters.Add(new SqlParameter("@schema", schema));
        }
        else
        {
            sql += " AND o_refd.name = @objeto AND SCHEMA_NAME(o_refd.schema_id) = @schema";
            parameters.Add(new SqlParameter("@objeto", objeto));
            parameters.Add(new SqlParameter("@schema", schema));
        }

        if (direcaoNorm == "ambos")
        {
            sql += " OR (o_refd.name = @objeto2 AND SCHEMA_NAME(o_refd.schema_id) = @schema2)";
            parameters.Add(new SqlParameter("@objeto2", objeto));
            parameters.Add(new SqlParameter("@schema2", schema));
        }

        sql += " ORDER BY referenciador_schema, referenciador, referenciado_schema, referenciado";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    [McpServerTool, Description("Visão geral do banco: contagens por tipo de objeto e TOP 20 maiores tabelas.")]
    public string ResumirBanco(string database = "")
    {
        const string sqlContagens = """
            SELECT o.type_desc AS categoria, COUNT(*) AS quantidade
            FROM sys.objects o
            WHERE o.type IN ('U', 'V', 'P', 'PC', 'FN', 'IF', 'TF', 'TR')
            GROUP BY o.type_desc
            ORDER BY quantidade DESC
            """;
        const string sqlMaiores = """
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
            """;
        var db = string.IsNullOrWhiteSpace(database) ? null : database;
        var contagens = executor.Execute(sqlContagens, db);
        var maiores = executor.Execute(sqlMaiores, db);
        return $"""
            === Contagens por tipo de objeto ===
            {contagens}

            === Maiores tabelas (TOP 20 por tamanho) ===
            {maiores}
            """;
    }

    [McpServerTool, Description("Amostra linhas de uma tabela (SELECT TOP N).")]
    public string AmostrarTabela(string tabela, string database = "", string schema = "dbo", int top = 20)
    {
        if (top < 1 || top > config.MaxRows)
        {
            return $"Bloqueado: top deve estar entre 1 e {config.MaxRows}.";
        }

        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(tabela))
        {
            return "Bloqueado: schema e tabela devem conter apenas letras, números e underscore.";
        }

        var sql = $"SELECT TOP ({top}) * FROM [{schema}].[{tabela}]";
        var error = ReadOnlyValidator.Validate(sql);
        if (error is not null)
        {
            return error;
        }

        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database);
    }

    [McpServerTool, Description("Perfil estatístico de uma coluna: nulos, distintos, min/max e amostras.")]
    public string PerfilColuna(string tabela, string coluna, string database = "", string schema = "dbo")
    {
        if (!IsSafeIdentifier(schema) || !IsSafeIdentifier(tabela) || !IsSafeIdentifier(coluna))
        {
            return "Bloqueado: identificadores devem conter apenas letras, números e underscore.";
        }

        const string metaSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tabela AND COLUMN_NAME = @coluna
            """;
        var db = string.IsNullOrWhiteSpace(database) ? null : database;
        var meta = executor.Execute(
            metaSql,
            db,
            new SqlParameter("@schema", schema),
            new SqlParameter("@tabela", tabela),
            new SqlParameter("@coluna", coluna));

        if (meta.Contains("(0 linha(s) exibida(s))", StringComparison.Ordinal))
        {
            return $"Coluna [{schema}].[{tabela}].[{coluna}] não encontrada.";
        }

        var dataType = meta.Split('\n')
            .Skip(2)
            .Select(line => line.Split('|').FirstOrDefault()?.Trim().ToLowerInvariant())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "desconhecido";

        var statsSql = $"""
            SELECT
                COUNT(*) AS total_linhas,
                SUM(CASE WHEN [{coluna}] IS NULL THEN 1 ELSE 0 END) AS nulos,
                COUNT(DISTINCT [{coluna}]) AS valores_distintos
            FROM [{schema}].[{tabela}]
            """;
        var stats = executor.Execute(statsSql, db);

        var extras = new List<string>();
        if (IsComparableType(dataType))
        {
            var minMaxSql = $"SELECT MIN([{coluna}]) AS minimo, MAX([{coluna}]) AS maximo FROM [{schema}].[{tabela}]";
            extras.Add("=== Min / Max ===");
            extras.Add(executor.Execute(minMaxSql, db));
        }

        var amostraSql = $"""
            SELECT TOP 5 [{coluna}] AS valor, COUNT(*) AS ocorrencias
            FROM [{schema}].[{tabela}]
            WHERE [{coluna}] IS NOT NULL
            GROUP BY [{coluna}]
            ORDER BY ocorrencias DESC
            """;
        extras.Add("=== Valores mais frequentes (TOP 5) ===");
        extras.Add(executor.Execute(amostraSql, db));

        return $"""
            === Perfil de [{schema}].[{tabela}].[{coluna}] (tipo: {dataType}) ===
            {stats}

            {string.Join("\n\n", extras)}
            """;
    }

    [McpServerTool, Description("Busca texto dentro de views, procedures e functions (sys.sql_modules).")]
    public string BuscarTextoSql(string termo, string database = "", string schema = "")
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return "Informe um termo de busca.";
        }

        var sql = """
            SELECT
                SCHEMA_NAME(o.schema_id) AS schema_name,
                o.name AS object_name,
                o.type_desc AS tipo,
                LEFT(m.definition, 500) AS trecho
            FROM sys.sql_modules m
            INNER JOIN sys.objects o ON o.object_id = m.object_id
            WHERE m.definition LIKE @termo
              AND o.type IN ('V', 'P', 'PC', 'FN', 'IF', 'TF')
            """;
        var parameters = new List<SqlParameter> { new("@termo", LikePattern(termo)) };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            sql += " AND SCHEMA_NAME(o.schema_id) = @schema";
            parameters.Add(new SqlParameter("@schema", schema));
        }
        sql += " ORDER BY schema_name, object_name";
        return executor.Execute(sql, string.IsNullOrWhiteSpace(database) ? null : database, parameters.ToArray());
    }

    private static bool IsComparableType(string dataType) =>
        dataType is "int" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric"
            or "float" or "real" or "money" or "smallmoney" or "date" or "datetime"
            or "datetime2" or "smalldatetime" or "time";

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');

    private static string LikePattern(string termo)
    {
        var escaped = termo
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
        return $"%{escaped}%";
    }
}
