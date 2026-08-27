"""Testes de integração das ferramentas Python contra SQL Server."""

from __future__ import annotations

import pytest

from mcp_sqlserver.server import (
    analisar_cobertura_indice,
    comparar_indices_redundantes,
    descrever_tabela,
    estimar_plano_consulta,
    executar_consulta,
    listar_fks_sem_indice,
    listar_indices,
    listar_tabelas,
    resumir_banco,
    usuario_conectado,
)

pytestmark = pytest.mark.integration


def _assert_sem_erro_sql(resultado: str) -> None:
    assert "@tabelaGROUP" not in resultado
    assert "@schemaGROUP" not in resultado
    assert not resultado.startswith("Erro SQL (137)"), resultado
    assert "unhandled exception" not in resultado.lower()


def test_usuario_conectado(test_database: str) -> None:
    resultado = usuario_conectado()
    _assert_sem_erro_sql(resultado)
    assert "modo_mcp" in resultado or "somente_leitura" in resultado


def test_listar_tabelas(test_database: str) -> None:
    resultado = listar_tabelas(database=test_database, schema="dbo")
    _assert_sem_erro_sql(resultado)
    assert "Pedidos" in resultado
    assert "Clientes" in resultado


def test_listar_indices_com_filtro_tabela(test_database: str) -> None:
    resultado = listar_indices(database=test_database, schema="dbo", tabela="Pedidos")
    _assert_sem_erro_sql(resultado)
    assert "IX_Pedidos_Status" in resultado


def test_listar_fks_sem_indice(test_database: str) -> None:
    resultado = listar_fks_sem_indice(database=test_database, schema="dbo")
    _assert_sem_erro_sql(resultado)
    assert "ClienteId" in resultado or "FK_Pedidos_Clientes" in resultado


def test_comparar_indices_redundantes(test_database: str) -> None:
    resultado = comparar_indices_redundantes(database=test_database, schema="dbo")
    _assert_sem_erro_sql(resultado)
    assert "duplicado" in resultado or "prefixo" in resultado or "0 linha(s)" in resultado


def test_analisar_cobertura_indice(test_database: str) -> None:
    resultado = analisar_cobertura_indice(
        tabela="Pedidos",
        colunas="Status",
        database=test_database,
        schema="dbo",
    )
    _assert_sem_erro_sql(resultado)
    assert "COMPLETA" in resultado or "PARCIAL" in resultado


def test_descrever_tabela(test_database: str) -> None:
    resultado = descrever_tabela(tabela="Pedidos", database=test_database, schema="dbo")
    _assert_sem_erro_sql(resultado)
    assert "Status" in resultado


def test_executar_consulta(test_database: str) -> None:
    resultado = executar_consulta(
        sql="SELECT TOP 1 Id, Status FROM dbo.Pedidos ORDER BY Id",
        database=test_database,
    )
    _assert_sem_erro_sql(resultado)
    assert "1 linha(s)" in resultado


def test_estimar_plano_consulta(test_database: str) -> None:
    resultado = estimar_plano_consulta(
        sql="SELECT Id FROM dbo.Pedidos WHERE Status = 'A'",
        database=test_database,
    )
    _assert_sem_erro_sql(resultado)
    assert "ShowPlanXML" in resultado or "Plano" in resultado or "RelOp" in resultado


def test_resumir_banco(test_database: str) -> None:
    resultado = resumir_banco(database=test_database)
    _assert_sem_erro_sql(resultado)
    assert "Contagens por tipo" in resultado
