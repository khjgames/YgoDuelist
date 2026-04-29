"""
Create C# card stubs for cards_database.json rows that have no matching sealed class yet.

Matching: same as sync_normal_monster_locales — normalize image_filename stem and class name with norm_key.

Naming (new classes): lowercase particles of/from/the/...; Card_ prefix when the identifier would start with a digit.
Acronyms from image stems: MK3 / mk_3 → MK_3, KA2 → KA_2, dna/ufo → DNA/UFO (matches PortraitPath .png slugs).

Stats: YGO ATK/DEF > 100 → divide by 100 (Cards_Revised.md). Otherwise use raw int.

Run from repo root:
  python tools/generate_missing_cards_from_db.py --dry-run
  python tools/generate_missing_cards_from_db.py --write

After adding new Fusion Monsters, run:
  python tools/sync_fusion_materials_from_db.py --write
  python tools/seed_card_locales_from_db.py --write --refresh-fusion-locales

New Ritual Spell stubs use RitualSpellCard from tools/ritual_spell_from_db.py; re-sync with:
  python tools/sync_ritual_spells_from_db.py --write
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "cards_database.json"
CARDS_CS_ROOT = ROOT / "YgoDuelistCode" / "Cards"

CLASS_RE = re.compile(r"public\s+sealed\s+class\s+(\w+)\s*:\s*(\w+)")

SMALL_WORDS = frozenset(
    {
        "of",
        "the",
        "from",
        "a",
        "an",
        "and",
        "or",
        "in",
        "on",
        "at",
        "to",
        "for",
        "with",
        "by",
        "vs",
        "v",
    }
)


def norm_key(s: str) -> str:
    s = s.lower().strip()
    s = re.sub(r"[^a-z0-9]+", "_", s)
    return s.strip("_")


_ACRONYM_PARTS = frozenset({"MK", "KA", "DNA", "UFO"})


def _expand_acronym_tokens(parts: list[str]) -> list[str]:
    """Turn mk3 / mk+3 / dna / ufo chunks into C# identifier segments (matches hand-authored cards)."""
    out: list[str] = []
    i = 0
    while i < len(parts):
        p = parts[i]
        pl = p.lower()
        if pl == "dna":
            out.append("DNA")
            i += 1
            continue
        if pl == "ufo":
            out.append("UFO")
            i += 1
            continue
        m = re.fullmatch(r"mk(\d+)", pl)
        if m:
            out.append("MK")
            out.append(m.group(1))
            i += 1
            continue
        m = re.fullmatch(r"ka(\d+)", pl)
        if m:
            out.append("KA")
            out.append(m.group(1))
            i += 1
            continue
        if pl == "mk" and i + 1 < len(parts) and parts[i + 1].isdigit():
            out.append("MK")
            out.append(parts[i + 1])
            i += 2
            continue
        if pl == "ka" and i + 1 < len(parts) and parts[i + 1].isdigit():
            out.append("KA")
            out.append(parts[i + 1])
            i += 2
            continue
        out.append(p)
        i += 1
    return out


def stem_to_class_name(stem: str) -> str:
    norm = re.sub(r"[^a-zA-Z0-9]+", "_", stem).strip("_").lower()
    parts = [p for p in norm.split("_") if p]
    if not parts:
        return "Invalid_Card"
    parts = _expand_acronym_tokens(parts)
    out: list[str] = []
    for i, p in enumerate(parts):
        if p.isdigit():
            out.append(p)
        elif p in _ACRONYM_PARTS:
            out.append(p)
        elif p in SMALL_WORDS and i > 0:
            out.append(p)
        else:
            out.append(p.capitalize())
    body = "_".join(out)
    if body[0].isdigit():
        return "Card_" + body
    return body


def scale_stat(v: int | None) -> int:
    if v is None:
        return 0
    if v > 100:
        return max(1, v // 100)
    return v


ATTR_MAP = {
    "EARTH": "Earth",
    "WATER": "Water",
    "FIRE": "Fire",
    "WIND": "Wind",
    "LIGHT": "Light",
    "DARK": "Dark",
    "DIVINE": "Light",
}

RACE_MAP = {
    "Aqua": "Aqua",
    "Beast": "Beast",
    "Beast-Warrior": "BeastWarrior",
    "Dinosaur": "Dinosaur",
    "Divine-Beast": "DivineBeast",
    "Dragon": "Dragon",
    "Fairy": "Fairy",
    "Fiend": "Fiend",
    "Fish": "Fish",
    "Insect": "Insect",
    "Machine": "Machine",
    "Plant": "Plant",
    "Psychic": "Psychic",
    "Pyro": "Pyro",
    "Reptile": "Reptile",
    "Rock": "Rock",
    "Sea Serpent": "SeaSerpent",
    "Spellcaster": "Spellcaster",
    "Thunder": "Thunder",
    "Warrior": "Warrior",
    "Winged Beast": "WingedBeast",
    "Wyrm": "Wyrm",
    "Zombie": "Zombie",
}


def map_race(r: str | None) -> str:
    if not r:
        return "Warrior"
    return RACE_MAP.get(r, "Warrior")


def map_attr(a: str | None) -> str:
    if not a:
        return "Earth"
    return ATTR_MAP.get(a.upper(), "Earth")


def load_existing_class_norm_keys() -> set[str]:
    keys: set[str] = set()
    for path in CARDS_CS_ROOT.rglob("*.cs"):
        if "Command" in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        for m in CLASS_RE.finditer(text):
            cls = m.group(1)
            keys.add(norm_key(cls))
            if cls.startswith("Card_"):
                keys.add(norm_key(cls[5:]))
    return keys


def monster_kind(card_type: str) -> str:
    t = card_type or ""
    if t == "Normal Monster" or t.endswith(" Normal Monster"):
        return "normal"
    if t == "Fusion Monster":
        return "fusion"
    if t in ("Ritual Monster", "Ritual Effect Monster"):
        return "ritual"
    if "Monster" in t:
        return "effect"
    return "effect"


def spell_race_to_folder_and_race(spell_race: str | None) -> tuple[str, str, str]:
    """Returns (subfolder, base_class, duel_race_enum)."""
    r = (spell_race or "Normal").strip()
    if r == "Field":
        return "Field", "BaseFieldSpellCard", "SpellField"
    if r == "Continuous":
        return "Continuos", "BaseContinuousSpellCard", "SpellContinuous"
    if r == "Equip":
        return "Equip", "BaseEquipSpellCard", "SpellEquip"
    if r == "Quick-Play":
        # Same namespace as other quick-plays (e.g. Super_Rejuvenation).
        return "Normal", "BaseSpellCard", "SpellQuickPlay"
    if r == "Ritual":
        return "Ritual", "BaseSpellCard", "SpellRitual"
    return "Normal", "BaseSpellCard", "SpellNormal"


def trap_race_to_folder_and_race(trap_race: str | None) -> tuple[str, str, str]:
    r = (trap_race or "Normal").strip()
    if r == "Continuous":
        return "Continuos", "BaseContinuousTrapCard", "TrapContinuous"
    if r == "Counter":
        return "Normal", "BaseTrapCard", "TrapCounter"
    return "Normal", "BaseTrapCard", "TrapNormal"


def render_normal_monster(cls: str, level: int, attr: str, race: str, atk: int, deff: int) -> str:
    return f"""using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;

public sealed class {cls} : NormalMonsterCard
{{
    public {cls}()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: {level},
            duelMonsterAttribute: DuelMonsterAttribute.{attr},
            baseAtk: {atk},
            baseDef: {deff},
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.{race})
    {{
    }}
}}
"""


def render_effect_monster(cls: str, level: int, attr: str, race: str, atk: int, deff: int) -> str:
    return f"""using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class {cls} : EffectMonsterCard
{{
    public {cls}()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: {level},
            duelMonsterAttribute: DuelMonsterAttribute.{attr},
            baseAtk: {atk},
            baseDef: {deff},
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.{race})
    {{
    }}
}}
"""


def render_fusion_monster(cls: str, level: int, attr: str, race: str, atk: int, deff: int) -> str:
    return f"""using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class {cls} : FusionMonsterCard
{{
    public {cls}()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: {level},
            duelMonsterAttribute: DuelMonsterAttribute.{attr},
            baseAtk: {atk},
            baseDef: {deff},
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.{race})
    {{
    }}
}}
"""


def render_ritual_monster(cls: str, level: int, attr: str, race: str, atk: int, deff: int) -> str:
    return f"""using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;

public sealed class {cls} : RitualMonsterCard
{{
    public {cls}()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: {level},
            duelMonsterAttribute: DuelMonsterAttribute.{attr},
            baseAtk: {atk},
            baseDef: {deff},
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.{race})
    {{
    }}
}}
"""


def render_spell_base(cls: str, ns_folder: str, base_class: str, duel_race: str) -> str:
    core_import = ""
    if base_class == "BaseFieldSpellCard":
        core_import = """
    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) => StatEffectTotal.None;
"""
    elif base_class == "BaseContinuousSpellCard":
        core_import = """
    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;
"""
    elif base_class == "BaseEquipSpellCard":
        core_import = """
    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;
"""
    if base_class == "BaseSpellCard":
        ctor = f"        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.{duel_race})"
    else:
        ctor = "        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)"
    uses_task = base_class != "BaseEquipSpellCard"
    task_using = "using System.Threading.Tasks;\n" if uses_task else ""
    on_spell = ""
    if base_class != "BaseEquipSpellCard":
        on_spell = """
    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;
"""
    return f"""{task_using}using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.{ns_folder};

public sealed class {cls} : {base_class}
{{
    public {cls}()
{ctor}
    {{
    }}
{core_import}{on_spell}
    protected override void OnUpgrade()
    {{
    }}
}}
"""


def render_trap_base(cls: str, ns_folder: str, base_class: str, duel_race: str) -> str:
    race_line = ""
    if base_class == "BaseTrapCard":
        race_line = f"        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.{duel_race})"
    else:
        race_line = "        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)"
    return f"""using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.{ns_folder};

public sealed class {cls} : {base_class}
{{
    public {cls}()
{race_line}
    {{
    }}

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {{
    }}
}}
"""


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    existing = load_existing_class_norm_keys()
    with open(DB_PATH, "r", encoding="utf-8") as f:
        data: list[dict] = json.load(f)

    sys.path.insert(0, str(ROOT / "tools"))
    from ritual_spell_from_db import (  # noqa: E402
        build_card_name_to_class,
        parse_ritual_spell_spec,
        render_ritual_spell_cs,
    )

    name_to_cls = build_card_name_to_class(data)

    planned: list[tuple[Path, str]] = []
    seen_stems: set[str] = set()
    skipped_dup: list[str] = []

    for card in data:
        fn = card.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        nk = norm_key(stem)
        if nk in seen_stems:
            skipped_dup.append(stem)
            continue
        seen_stems.add(nk)

        if nk in existing:
            continue

        cls = stem_to_class_name(stem)
        ctype = card.get("type") or ""
        attr = map_attr(card.get("attribute"))
        race = map_race(card.get("race"))
        level = int(card.get("level") or 1)
        atk = scale_stat(card.get("atk"))
        deff = scale_stat(card.get("def"))

        if ctype == "Spell Card":
            sub, base_c, dr = spell_race_to_folder_and_race(card.get("race"))
            if sub == "Ritual":
                try:
                    spec = parse_ritual_spell_spec(card.get("desc") or "", name_to_cls)
                except ValueError as e:
                    raise SystemExit(f"Ritual spell {cls}: {e}") from e
                body = render_ritual_spell_cs(cls, spec)
            else:
                body = render_spell_base(cls, sub, base_c, dr)
            rel = Path("Spell") / "Todo" / sub / f"{cls}.cs"
        elif ctype == "Trap Card":
            sub, base_c, dr = trap_race_to_folder_and_race(card.get("race"))
            body = render_trap_base(cls, sub, base_c, dr)
            rel = Path("Trap") / "Todo" / sub / f"{cls}.cs"
        else:
            kind = monster_kind(ctype)
            if kind == "normal":
                body = render_normal_monster(cls, level, attr, race, atk, deff)
                rel = Path("Monster") / "Todo" / "Normal" / f"{cls}.cs"
            elif kind == "fusion":
                body = render_fusion_monster(cls, level, attr, race, atk, deff)
                rel = Path("Monster") / "Done" / "Fusion" / f"{cls}.cs"
            elif kind == "ritual":
                body = render_ritual_monster(cls, level, attr, race, atk, deff)
                rel = Path("Monster") / "Done" / "Ritual" / f"{cls}.cs"
            else:
                body = render_effect_monster(cls, level, attr, race, atk, deff)
                rel = Path("Monster") / "Todo" / "Effect" / f"{cls}.cs"

        path = CARDS_CS_ROOT / rel
        planned.append((path, body))

    print("Existing norm_keys (card classes):", len(existing))
    print("Planned new files:", len(planned))
    print("Skipped duplicate stems in DB:", len(skipped_dup))
    if planned[:5]:
        for p, _ in planned[:5]:
            print("  example:", p.relative_to(ROOT))

    if args.write:
        for path, body in planned:
            path.parent.mkdir(parents=True, exist_ok=True)
            if path.exists():
                print("skip exists:", path.relative_to(ROOT))
                continue
            path.write_text(body, encoding="utf-8")
            print("wrote", path.relative_to(ROOT))
    elif not args.dry_run:
        print("Use --write to create files, or --dry-run to silence this.")
        print("First 20 paths:")
        for path, _ in planned[:20]:
            print(path.relative_to(ROOT))


if __name__ == "__main__":
    main()
