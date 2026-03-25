import os
import re
import subprocess
import sys


def main() -> int:
    out = subprocess.check_output(
        [sys.executable, os.path.join("tools", "map_revised_cards_to_code.py")],
        text=True,
        encoding="utf-8",
        errors="ignore",
    )

    paths: list[str] = []
    for line in out.splitlines():
        if "\t" not in line:
            continue
        if line.startswith("OK:") or line.startswith("Missing"):
            continue
        parts = line.split("\t")
        if len(parts) >= 4 and parts[3].strip().endswith(".cs"):
            paths.append(parts[3].strip())

    root = os.path.abspath(".")
    report: list[tuple[int, int, bool, str]] = []
    for rel in paths:
        p = os.path.join(root, rel)
        try:
            txt = open(p, "r", encoding="utf-8", errors="ignore").read()
        except OSError:
            report.append((0, 0, True, f"MISSING\t{rel}"))
            continue

        loc = txt.count("\n") + 1
        overrides = len(re.findall(r"\boverride\b", txt))
        placeholder = "Placeholder" in txt
        report.append((loc, overrides, placeholder, rel))

    report.sort(key=lambda x: (x[0], x[1], x[2]))
    for loc, overrides, placeholder, rel in report:
        flag = "placeholder" if placeholder else ""
        print(f"{loc:4d} lines ov={overrides:2d} {flag:11s} {rel}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

