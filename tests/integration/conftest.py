"""Fixtures compartilhadas para testes de integração com SQL Server."""

from __future__ import annotations

import os
from pathlib import Path

import pyodbc
import pytest

TEST_DATABASE = "McpSqlServerTest"
INTEGRATION_ENV = "MCPMSSQL_TEST_CONNECTION_STRING"


def _connection_string() -> str | None:
    value = os.environ.get(INTEGRATION_ENV, "").strip()
    return value or None


def pytest_configure(config: pytest.Config) -> None:
    config.addinivalue_line(
        "markers",
        "integration: requer SQL Server (variável MCPMSSQL_TEST_CONNECTION_STRING)",
    )


def pytest_collection_modifyitems(config: pytest.Config, items: list[pytest.Item]) -> None:
    if _connection_string():
        return
    skip = pytest.mark.skip(
        reason=f"Defina {INTEGRATION_ENV} para executar testes de integração."
    )
    for item in items:
        if "integration" in item.keywords:
            item.add_marker(skip)


@pytest.fixture(scope="session")
def integration_connection_string() -> str:
    connection_string = _connection_string()
    if not connection_string:
        pytest.skip(f"Defina {INTEGRATION_ENV} para executar testes de integração.")
    return connection_string


@pytest.fixture(scope="session")
def seeded_database(integration_connection_string: str) -> str:
    master = pyodbc.connect(integration_connection_string, autocommit=True, timeout=15)

    with master.cursor() as cursor:
        cursor.execute(
            f"IF DB_ID('{TEST_DATABASE}') IS NULL CREATE DATABASE [{TEST_DATABASE}]"
        )

    test_conn_str = _with_database(integration_connection_string, TEST_DATABASE)
    conn = pyodbc.connect(test_conn_str, autocommit=True, timeout=15)
    try:
        _seed_schema(conn)
    finally:
        conn.close()

    return TEST_DATABASE


def _with_database(connection_string: str, database: str) -> str:
    parts = [part for part in connection_string.split(";") if part.strip()]
    filtered = [part for part in parts if not part.lower().startswith("database=")]
    filtered.append(f"Database={database}")
    return ";".join(filtered) + ";"


def _seed_schema(conn: pyodbc.Connection) -> None:
    seed_file = Path(__file__).with_name("seed.sql")
    sql = seed_file.read_text(encoding="utf-8")
    batches = [batch.strip() for batch in sql.split("\nGO\n") if batch.strip()]
    with conn.cursor() as cursor:
        for batch in batches:
            cursor.execute(batch)


@pytest.fixture()
def test_database(seeded_database: str) -> str:
    return seeded_database
