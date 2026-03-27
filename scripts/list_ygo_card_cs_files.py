#!/usr/bin/env python3
"""
Walk YgoDuelistCode/Cards (or a given root) and print one alphabetical list per folder
that contains at least one .cs file. Category key = path relative to the cards root.

Usage:
  python scripts/list_ygo_card_cs_files.py
  python scripts/list_ygo_card_cs_files.py "C:\\path\\to\\YgoDuelistCode\\Cards"
  python scripts/list_ygo_card_cs_files.py --out card_lists.txt
"""

from __future__ import annotations

import argparse
from collections import defaultdict
from pathlib import Path
from typing import Optional


def find_repo_cards_root(start: Path) -> Optional[Path]:
    """If start is inside the repo, prefer .../YgoDuelistCode/Cards when it exists."""
    for p in [start, *start.parents]:
        candidate = p / "YgoDuelistCode" / "Cards"
        if candidate.is_dir():
            return candidate.resolve()
    return None


def collect_by_folder(cards_root: Path) -> dict[str, list[str]]:
    """Map relative folder path (posix) -> sorted basenames of .cs files in that folder only."""
    by_folder: dict[str, list[str]] = defaultdict(list)
    cards_root = cards_root.resolve()

    for path in cards_root.rglob("*.cs"):
        if not path.is_file():
            continue
        parent = path.parent.resolve()
        try:
            rel_parent = parent.relative_to(cards_root)
        except ValueError:
            continue
        key = rel_parent.as_posix() if str(rel_parent) != "." else "."
        by_folder[key].append(path.name)

    for key in by_folder:
        by_folder[key].sort(key=str.lower)

    return dict(sorted(by_folder.items(), key=lambda kv: kv[0].lower()))


def main() -> None:
    parser = argparse.ArgumentParser(description="Alphabetical .cs lists per Cards subfolder.")
    parser.add_argument(
        "cards_root",
        nargs="?",
        default=None,
        help="Path to YgoDuelistCode/Cards (default: YgoDuelistCode/Cards next to cwd or script)",
    )
    parser.add_argument(
        "--out",
        "-o",
        type=Path,
        default=None,
        help="Write output to this file instead of stdout (UTF-8).",
    )
    args = parser.parse_args()

    if args.cards_root:
        root = Path(args.cards_root).expanduser().resolve()
    else:
        cwd = Path.cwd()
        script_dir = Path(__file__).resolve().parent
        root = find_repo_cards_root(cwd) or find_repo_cards_root(script_dir)
        if root is None:
            root = (script_dir.parent / "YgoDuelistCode" / "Cards").resolve()

    if not root.is_dir():
        raise SystemExit(f"Cards root not found or not a directory: {root}")

    by_folder = collect_by_folder(root)
    total_cs = sum(len(names) for names in by_folder.values())

    lines: list[str] = []
    lines.append(f"# Cards root: {root}")
    lines.append(f"# Folders with .cs: {len(by_folder)}")
    lines.append(f"# Total .cs files: {total_cs}")
    lines.append("")

    for folder_key, names in by_folder.items():
        lines.append(f"## {folder_key}")
        lines.extend(names)
        lines.append("")

    text = "\n".join(lines).rstrip() + "\n"

    if args.out:
        args.out.write_text(text, encoding="utf-8")
        print(f"Wrote {args.out} ({total_cs} files in {len(by_folder)} folders).")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()
