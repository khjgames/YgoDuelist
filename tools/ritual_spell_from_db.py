"""
Parse cards_database.json ritual spell descriptions and emit RitualSpellCard C#.

Shared by sync_ritual_spells_from_db.py and generate_missing_cards_from_db.py.
"""
from __future__ import annotations

import re
from dataclasses import dataclass

from generate_missing_cards_from_db import stem_to_class_name


@dataclass(frozen=True)
class RitualSpellSpec:
    target_type_expr: str  # C# typeof(...) inner, e.g. "Black_Luster_Soldier" -> typeof(Black_Luster_Soldier)
    needs_monster_import: bool
    level_requirement: int
    material_compare: str  # "AtLeast" | "Exact"
    attr_filter: str | None  # DuelMonsterAttribute name
    use_target_level: bool
    exact_material_count: int | None


RITUAL_WORD_ATTR = {
    "EARTH": "Earth",
    "WATER": "Water",
    "FIRE": "Fire",
    "WIND": "Wind",
    "LIGHT": "Light",
    "DARK": "Dark",
    "DIVINE": "Light",
}


def build_card_name_to_class(data: list[dict]) -> dict[str, str]:
    """Map official card name -> C# class name (from image stem)."""
    out: dict[str, str] = {}
    for card in data:
        fn = card.get("image_filename") or ""
        if not fn.endswith(".jpg"):
            continue
        stem = fn[:-4]
        cls = stem_to_class_name(stem)
        name = (card.get("name") or "").strip()
        if name:
            out[name] = cls
    return out


def _class_for_ritual_target_name(name: str, name_to_cls: dict[str, str]) -> str:
    if name in name_to_cls:
        return name_to_cls[name]
    slug = re.sub(r"[^a-zA-Z0-9]+", "_", name.strip()).strip("_").lower()
    return stem_to_class_name(slug)


def _parse_material_rule(desc_one_line: str) -> tuple[int, str, bool, int | None]:
    """
    Returns (level_requirement, material_compare AtLeast|Exact, use_target_level, exact_material_count).
    """
    d = desc_one_line
    if re.search(r"exactly equal the Level of the Ritual Monster", d, re.I):
        return 0, "Exact", True, None

    m = re.search(
        r"Tribute\s+a\s+monster[^.]*?Level\s+is\s+(\d+)\s+or\s+more",
        d,
        re.I,
    )
    if m:
        return int(m.group(1)), "AtLeast", False, 1

    m = re.search(r"Level\s+Stars\s+equal\s+(\d+)\s+or\s+more", d, re.I)
    if m:
        return int(m.group(1)), "AtLeast", False, None

    m = re.search(r"total\s+Levels\s+equal\s+(\d+)\s+or\s+more", d, re.I)
    if m:
        return int(m.group(1)), "AtLeast", False, None

    m = re.search(r"Levels\s+equal\s+(\d+)\s+or\s+more", d, re.I)
    if m:
        return int(m.group(1)), "AtLeast", False, None

    raise ValueError(f"Could not parse ritual material rule from: {d[:200]}...")


def parse_ritual_spell_spec(desc: str, name_to_cls: dict[str, str]) -> RitualSpellSpec:
    d = (desc or "").replace("\r\n", "\n")
    one = " ".join(d.split())

    m_attr = re.search(r"any\s+(\w+)\s+Ritual\s+Monster", one, re.I)
    if m_attr:
        raw = m_attr.group(1).upper()
        cs_attr = RITUAL_WORD_ATTR.get(raw)
        if not cs_attr:
            raise ValueError(f"Unknown attribute word in ritual spell: {raw}")
        lvl, cmp_, use_tgt, exact = _parse_material_rule(one)
        return RitualSpellSpec(
            target_type_expr="RitualMonsterCard",
            needs_monster_import=False,
            level_requirement=lvl,
            material_compare=cmp_,
            attr_filter=cs_attr,
            use_target_level=use_tgt,
            exact_material_count=exact,
        )

    m_q = re.search(r'Ritual\s+Summon\s+"([^"]+)"', one, re.I)
    if not m_q:
        raise ValueError(f"No Ritual Summon quoted target in: {one[:200]}...")

    target_name = m_q.group(1).strip()
    monster_cls = _class_for_ritual_target_name(target_name, name_to_cls)
    lvl, cmp_, use_tgt, exact = _parse_material_rule(one)
    return RitualSpellSpec(
        target_type_expr=monster_cls,
        needs_monster_import=True,
        level_requirement=lvl,
        material_compare=cmp_,
        attr_filter=None,
        use_target_level=use_tgt,
        exact_material_count=exact,
    )


def render_ritual_spell_cs(spell_class_name: str, spec: RitualSpellSpec) -> str:
    monster_ns = "YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual"
    import_lines = [
        "using MegaCrit.Sts2.Core.Entities.Cards;",
        "using MegaCrit.Sts2.Core.Models;",
        "using YgoDuelist.YgoDuelistCode.Cards.Core;",
    ]
    if spec.needs_monster_import:
        import_lines.append(f"using {monster_ns};")
    import_lines.append("using YgoDuelist.YgoDuelistCode.Models;")

    optional_lines: list[str] = []
    if spec.attr_filter:
        optional_lines.append(f"            ritualTargetAttributeFilter: DuelMonsterAttribute.{spec.attr_filter}")
    if spec.use_target_level:
        optional_lines.append("            useRitualTargetLevelAsMaterialRequirement: true")
    if spec.exact_material_count == 1:
        optional_lines.append("            exactMaterialCardCount: 1")

    for i in range(len(optional_lines) - 1):
        optional_lines[i] = optional_lines[i] + ","

    mat_line = f"            materialLevelCompare: RitualMaterialLevelCompare.{spec.material_compare}"
    if optional_lines:
        mat_line += ","
    optional_block = ("\n" + "\n".join(optional_lines)) if optional_lines else ""

    body = "\n".join(import_lines)
    body += f"""

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class {spell_class_name} : RitualSpellCard
{{
    public {spell_class_name}()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof({spec.target_type_expr}),
            levelRequirement: {spec.level_requirement},
{mat_line}{optional_block})
    {{
    }}

    protected override void OnUpgrade()
    {{
        ExecuteSpellUpgradePlaceholder();
    }}

    private void ExecuteSpellUpgradePlaceholder()
    {{
    }}
}}
"""
    return body.lstrip()
