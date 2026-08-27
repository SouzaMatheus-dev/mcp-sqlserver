"""Testes de regressão estática para evitar bugs de montagem SQL no .NET."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SQL_TOOLS = ROOT / "dotnet" / "McpSqlServer" / "Tools" / "SqlTools.cs"


def test_listar_indices_nao_concatena_parametro_com_group() -> None:
    source = SQL_TOOLS.read_text(encoding="utf-8")
    assert ' AND t.name = @tabela\\n"' in source
    assert "@tabelaGROUP" not in source


def test_listar_colunas_candidatas_nao_concatena_parametro_com_group() -> None:
    source = SQL_TOOLS.read_text(encoding="utf-8")
    assert " AND SCHEMA_NAME(t.schema_id) = @schema\\n\"" in source
    assert "@schemaGROUP" not in source


def test_sql_executor_trata_sql_exception() -> None:
    source = (ROOT / "dotnet" / "McpSqlServer" / "Services" / "SqlExecutor.cs").read_text(
        encoding="utf-8"
    )
    assert "catch (SqlException" in source
    assert "SqlErrorFormatter.Format" in source
