import pytest

from mcp_sqlserver.readonly import validar_consulta_leitura


@pytest.mark.parametrize(
    "sql",
    [
        "SELECT 1",
        "WITH cte AS (SELECT 1 AS n) SELECT * FROM cte",
        "SELECT * FROM dbo.Clientes WHERE id = 1",
    ],
)
def test_consultas_permitidas(sql: str) -> None:
    assert validar_consulta_leitura(sql) is None


@pytest.mark.parametrize(
    "sql",
    [
        "DELETE FROM tabela",
        "INSERT INTO tabela VALUES (1)",
        "SELECT * INTO backup FROM tabela",
        "EXEC sp_help",
        "SELECT 1; DROP TABLE t",
        "SELECT 1 UNION SELECT * FROM OPENROWSET('SQLNCLI', 'server=.', 'SELECT 1')",
    ],
)
def test_consultas_bloqueadas(sql: str) -> None:
    assert validar_consulta_leitura(sql) is not None


def test_comentarios_nao_bypassam_validacao() -> None:
    sql = "/* ok */ SELECT 1; DELETE FROM t"
    assert validar_consulta_leitura(sql) is not None
