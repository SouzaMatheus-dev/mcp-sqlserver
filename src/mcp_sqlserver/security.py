import re


def identificador_seguro(nome: str) -> str | None:
    if not nome or not re.match(r"^[A-Za-z_][A-Za-z0-9_]*$", nome):
        return None
    return f"[{nome}]"
