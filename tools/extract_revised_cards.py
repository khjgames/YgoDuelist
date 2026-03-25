import pathlib


def main() -> None:
    root = pathlib.Path(__file__).resolve().parents[1]
    md_path = root / "Cards_Revised.md"
    lines = md_path.read_text(encoding="utf-8").splitlines()

    cards: list[str] = []
    for ln in lines:
        s = ln.strip()
        if s.lower().startswith("generic upgrade"):
            break
        if not s:
            continue
        if s.startswith("Listing") or s.startswith("You will") or s.startswith("Important"):
            continue

        if "->" in s:
            name = s.split("->", 1)[0].strip()
            if name:
                cards.append(name)
            continue

        if " - " in s and s[0].isalpha():
            # e.g. "archfiend's oath - once per turn ..." / "blind destruction - annual - ..."
            name = s.split(" - ", 1)[0].strip()
            if name:
                cards.append(name)

    # de-dupe, preserve order
    seen: set[str] = set()
    out: list[str] = []
    for c in cards:
        k = c.lower()
        if k in seen:
            continue
        seen.add(k)
        out.append(c)

    print(len(out))
    for c in out:
        print(c)


if __name__ == "__main__":
    main()

