"""Compatibilidade com configurações que apontam para server.py na raiz."""

from mcp_sqlserver.server import run

if __name__ == "__main__":
    run()
