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

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
}
