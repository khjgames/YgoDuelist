import json
import keyword
import re
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
CARDS_JSON = REPO_ROOT / "cards.json"
CARDS_ROOT = REPO_ROOT / "YgoDuelistCode" / "Cards"


def to_identifier(name: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9]+", "_", name).strip("_")
    if not cleaned:
        cleaned = "Card"
    if cleaned[0].isdigit() or keyword.iskeyword(cleaned):
        cleaned = f"Card_{cleaned}"
    return cleaned


def normalized(name: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", name.lower())


def attr_to_enum(value: str | None) -> str:
    mapping = {
        "EARTH": "Earth",
        "WATER": "Water",
        "FIRE": "Fire",
        "WIND": "Wind",
        "LIGHT": "Light",
        "DARK": "Dark",
    }
    return mapping.get((value or "").upper(), "Earth")


def classify(card_type: str, race: str | None) -> tuple[str, str, str]:
    race_value = (race or "").strip().lower()

    if "monster" in card_type.lower():
        if "fusion" in card_type.lower():
            return "Monster/Todo/Fusion", "FusionMonsterCard", "Monster"
        if "ritual" in card_type.lower():
            return "Monster/Todo/Ritual", "RitualMonsterCard", "Monster"
        if "normal" in card_type.lower():
            return "Monster/Todo/Normal", "NormalMonsterCard", "Monster"
        return "Monster/Todo/Effect", "EffectMonsterCard", "Monster"

    if "spell" in card_type.lower():
        if race_value == "field":
            return "Spell/Todo/Field", "BaseSpellCard", "Spell"
        if race_value == "equip":
            return "Spell/Todo/Equip", "BaseSpellCard", "Spell"
        if race_value == "continuous":
            return "Spell/Todo/Continuos", "BaseSpellCard", "Spell"
        return "Spell/Todo/Normal", "BaseSpellCard", "Spell"

    if "trap" in card_type.lower():
        if race_value == "equip":
            return "Trap/Todo/Equip", "BaseTrapCard", "Trap"
        if race_value == "continuous":
            return "Trap/Todo/Continuos", "BaseTrapCard", "Trap"
        return "Trap/Todo/Normal", "BaseTrapCard", "Trap"

    return "Spell/Todo/Normal", "BaseSpellCard", "Spell"


def build_monster_source(namespace: str, class_name: str, base_class: str, level: int, attr: str, atk: int, defense: int) -> str:
    return f"""using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace {namespace};

public sealed class {class_name} : {base_class}
{{
    public {class_name}()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: {level},
            duelMonsterAttribute: DuelMonsterAttribute.{attr},
            baseAtk: {atk},
            baseDef: {defense},
            baseMgc: 0)
    {{
    }}

    protected override void OnUpgrade()
    {{
        ApplyCardEffectPlaceholder();
    }}

    private void ApplyCardEffectPlaceholder()
    {{
    }}
}}
"""


def build_spell_source(namespace: str, class_name: str, target: str) -> str:
    return f"""using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace {namespace};

public sealed class {class_name} : BaseSpellCard
{{
    public {class_name}()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.{target})
    {{
    }}

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {{
        ExecuteSpellEffectPlaceholder(choiceContext, cardPlay);
        return Task.CompletedTask;
    }}

    protected override void OnUpgrade()
    {{
        ExecuteSpellUpgradePlaceholder();
    }}

    private void ExecuteSpellEffectPlaceholder(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {{
    }}

    private void ExecuteSpellUpgradePlaceholder()
    {{
    }}
}}
"""


def build_trap_source(namespace: str, class_name: str, target: str) -> str:
    return f"""using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace {namespace};

public sealed class {class_name} : BaseTrapCard
{{
    public {class_name}()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.{target})
    {{
    }}

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {{
        ExecuteTrapEffectPlaceholder(choiceContext, cardPlay);
        return Task.CompletedTask;
    }}

    protected override void OnUpgrade()
    {{
        ExecuteTrapUpgradePlaceholder();
    }}

    private void ExecuteTrapEffectPlaceholder(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {{
    }}

    private void ExecuteTrapUpgradePlaceholder()
    {{
    }}
}}
"""


def main() -> None:
    cards = json.loads(CARDS_JSON.read_text(encoding="utf-8"))

    all_cs_files = list(CARDS_ROOT.rglob("*.cs"))
    existing_stems = {path.stem for path in all_cs_files}
    existing_normalized = {normalized(path.stem) for path in all_cs_files}

    created = 0
    skipped_existing = 0
    created_by_folder: dict[str, int] = {}
    used_names = set(existing_stems)

    for card in cards:
        name = card.get("name", "")
        card_type = card.get("type", "")
        race = card.get("race")
        rel_folder, base_class, card_group = classify(card_type, race)
        class_name = to_identifier(name)

        if normalized(class_name) in existing_normalized:
            skipped_existing += 1
            continue

        if class_name in used_names:
            class_name = f"{class_name}_{card.get('id', 'Card')}"
            if normalized(class_name) in existing_normalized:
                skipped_existing += 1
                continue

        target_dir = CARDS_ROOT / rel_folder
        target_dir.mkdir(parents=True, exist_ok=True)
        target_file = target_dir / f"{class_name}.cs"
        namespace = "YgoDuelist.YgoDuelistCode.Cards." + rel_folder.replace("/", ".")

        if card_group == "Monster":
            level = int(card.get("level") or 4)
            atk = int(card.get("atk") or 0)
            defense = int(card.get("def") or 0)
            attr = attr_to_enum(card.get("attribute"))
            source = build_monster_source(namespace, class_name, base_class, level, attr, atk, defense)
        elif card_group == "Spell":
            target = "AnyEnemy" if (race or "").strip().lower() == "equip" else "Self"
            source = build_spell_source(namespace, class_name, target)
        else:
            target = "AnyEnemy" if (race or "").strip().lower() == "equip" else "Self"
            source = build_trap_source(namespace, class_name, target)

        target_file.write_text(source, encoding="utf-8", newline="\n")
        used_names.add(class_name)
        existing_normalized.add(normalized(class_name))
        created += 1
        created_by_folder[rel_folder] = created_by_folder.get(rel_folder, 0) + 1

    print(f"Created stubs: {created}")
    print(f"Skipped existing: {skipped_existing}")
    for folder in sorted(created_by_folder):
        print(f"{folder}: {created_by_folder[folder]}")


if __name__ == "__main__":
    main()
