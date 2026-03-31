"""Ensure PackTags always starts with YgoCardPackTags.Starter (excludes Command folder)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "YgoDuelistCode" / "Cards"


def rebuild_expr(expr: str) -> str:
    expr = expr.strip().rstrip(";").strip()
    if expr == "YgoCardPackTags.None":
        return "YgoCardPackTags.Starter"
    tokens = re.findall(r"YgoCardPackTags\.\w+", expr)
    if not tokens:
        return "YgoCardPackTags.Starter"
    starter = "YgoCardPackTags.Starter"
    rest: list[str] = []
    for t in tokens:
        if t == starter:
            continue
        if t not in rest:
            rest.append(t)
    return " | ".join([starter] + rest)


def fix_content(text: str) -> tuple[str, bool]:
    changed = False

    def repl_ml(m: re.Match[str]) -> str:
        nonlocal changed
        old = m.group(2).strip().rstrip(";").strip()
        new_e = rebuild_expr(m.group(2))
        if new_e != old:
            changed = True
        return f"{m.group(1)}\n        {new_e};"

    text = re.sub(
        r"(    public override YgoCardPackTags PackTags =>)\s*\n\s*([^;]+);",
        repl_ml,
        text,
    )

    def repl_sl(m: re.Match[str]) -> str:
        nonlocal changed
        old = m.group(1).strip().rstrip(";").strip()
        new_e = rebuild_expr(m.group(1))
        if new_e != old:
            changed = True
        return f"    public override YgoCardPackTags PackTags => {new_e};"

    text = re.sub(
        r"^    public override YgoCardPackTags PackTags => ([^;\n]+);\s*$",
        repl_sl,
        text,
        flags=re.MULTILINE,
    )
    return text, changed


def main() -> None:
    n_files = 0
    for path in sorted(ROOT.rglob("*.cs")):
        if "Command" in path.parts:
            continue
        raw = path.read_text(encoding="utf-8")
        new, ch = fix_content(raw)
        if ch:
            path.write_text(new, encoding="utf-8")
            n_files += 1
            print(path.relative_to(ROOT))
    print(f"Updated {n_files} files.")


if __name__ == "__main__":
    main()
