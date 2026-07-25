"""Validação de SQL para uso corporativo em modo somente leitura."""

import re

BLOCKED_KEYWORDS = (
    "INSERT",
    "UPDATE",
    "DELETE",
    "MERGE",
    "TRUNCATE",
    "DROP",
    "CREATE",
    "ALTER",
    "RENAME",
    "EXEC",
    "EXECUTE",
    "GRANT",
    "REVOKE",
    "DENY",
    "BULK",
    "OPENROWSET",
    "OPENDATASOURCE",
    "OPENQUERY",
    "OPENXML",
    "SHUTDOWN",
    "DBCC",
    "KILL",
    "RECONFIGURE",
    "WAITFOR",
)

BLOCKED_PATTERNS = (
    re.compile(r"\bsp_executesql\b", re.IGNORECASE),
    re.compile(r"\bxp_\w+\b", re.IGNORECASE),
    re.compile(r"\bINTO\s+", re.IGNORECASE),
    re.compile(r"\bBACKUP\s+(DATABASE|LOG)\b", re.IGNORECASE),
    re.compile(r"\bRESTORE\s+(DATABASE|LOG)\b", re.IGNORECASE),
    re.compile(r";\s*\S", re.IGNORECASE),
)

ALLOWED_START = re.compile(r"^(SELECT|WITH)\b", re.IGNORECASE)


def _remover_comentarios(sql: str) -> str:
    sem_bloco = re.sub(r"/\*.*?\*/", " ", sql, flags=re.DOTALL)
    return re.sub(r"--[^\n\r]*", " ", sem_bloco)


def _normalizar(sql: str) -> str:
    return re.sub(r"\s+", " ", _remover_comentarios(sql)).strip()


def validar_consulta_leitura(sql: str) -> str | None:
    """Retorna mensagem de erro se a consulta não for permitida; None se OK."""
    normalizado = _normalizar(sql)
    if not normalizado:
        return "Consulta vazia não é permitida."

    if not ALLOWED_START.match(normalizado):
        return (
            "Bloqueado: em modo corporativo somente leitura só são permitidas "
            "consultas que começam com SELECT ou WITH."
        )

    upper = normalizado.upper()
    for keyword in BLOCKED_KEYWORDS:
        if re.search(rf"\b{keyword}\b", upper):
            return f"Bloqueado: palavra-chave não permitida em modo leitura: {keyword}."

    for pattern in BLOCKED_PATTERNS:
        if pattern.search(normalizado):
            return (
                "Bloqueado: padrão SQL não permitido em modo leitura "
                f"({pattern.pattern})."
            )

    return None
