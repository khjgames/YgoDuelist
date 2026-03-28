# Apply Starter pack tags, RelatedCards (self), commented BundledCards, rarity — Planned_Starter_Cards lines 1-42.
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1] / "YgoDuelistCode" / "Cards"

BLOCK_TMPL = """    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => {tags};

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //}};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {{
        typeof({cls}),
    }};

"""

# (rel_path, class_name, tags_expr, rarity or None, unique_marker_after_ctor_closing_brace)


def ensure_usings(text: str) -> str:
    ns = text.find("namespace ")
    pre = text[:ns]
    rest = text[ns:]
    if "using System;" not in pre:
        pre = "using System;\n" + pre
    if "using YgoDuelist.YgoDuelistCode.Cards;" not in pre:
        if "using YgoDuelist.YgoDuelistCode.Cards.Core;" in pre:
            pre = pre.replace(
                "using YgoDuelist.YgoDuelistCode.Cards.Core;",
                "using YgoDuelist.YgoDuelistCode.Cards;\nusing YgoDuelist.YgoDuelistCode.Cards.Core;",
                1,
            )
        else:
            pre = pre + "\nusing YgoDuelist.YgoDuelistCode.Cards;\n"
    return pre + rest


def apply_rarity(text: str, rarity: str) -> str:
    r = f"CardRarity.{rarity}"
    m = re.search(r"rarity:\s*CardRarity\.\w+", text)
    if m:
        return text[: m.start()] + f"rarity: {r}" + text[m.end() :]
    m = re.search(r":\s*base\(\s*[^,)]+,\s*CardRarity\.\w+", text)
    if m:
        inner = re.sub(r"CardRarity\.\w+", r, m.group(0), count=1)
        return text[: m.start()] + inner + text[m.end() :]
    raise ValueError("no rarity slot found")


def block_for(cls: str, tags: str) -> str:
    return BLOCK_TMPL.format(cls=cls, tags=tags)


DATA = [
    ("Trap/Todo/Continuos/Bad_Reaction_to_Simochi.cs", "Bad_Reaction_to_Simochi",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn", None,
     "    }\n\n    protected override Task OnTrapPlay"),
    ("Spell/Todo/Normal/Upstart_Goblin.cs", "Upstart_Goblin",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search | YgoCardPackTags.Burn", None,
     "    }\n\n    protected override async Task OnSpellPlay"),
    ("Trap/Todo/Normal/Dark_Mirror_Force.cs", "Dark_Mirror_Force",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn", None,
     "    }\n\n    protected override async Task OnTrapPlay"),
    ("Trap/Todo/Normal/Enchanted_Javelin.cs", "Enchanted_Javelin",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Healing", None,
     "    }\n\n    protected override bool IsPlayable"),
    ("Spell/Todo/Continuos/Stumbling.cs", "Stumbling",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell", None,
     "    }\n\n    public override StatEffectTotal GetContinuousStatEffect"),
    ("Trap/Todo/Continuos/Tornado_Wall.cs", "Tornado_Wall",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap", None,
     "    }\n\n    protected override async Task OnTrapPlay"),
    ("Trap/Todo/Normal/Spell_Shield_Type_8.cs", "Spell_Shield_Type_8",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap", None,
     "    }\n\n    protected override async Task OnTrapPlay"),
    ("Trap/Todo/Continuos/Skull_Invitation.cs", "Skull_Invitation",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn", None,
     "    }\n\n    protected override Task OnTrapPlay"),
    ("Spell/Todo/Normal/Shield_Sword.cs", "Shield_Sword",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell", None,
     "    }\n\n    protected override Task OnSpellPlay"),
    ("Trap/Todo/Normal/Secret_Barrel.cs", "Secret_Barrel",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn", None,
     "    }\n\n    protected override Task OnTrapPlay"),
    ("Spell/Todo/Equip/Dragon_Nails.cs", "Dragon_Nails",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Dragon", "Uncommon",
     "    }\n\n    public override bool CanEquipTo"),
    ("Spell/Todo/Normal/Spellbook_Organization.cs", "Spellbook_Organization",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", "Uncommon",
     "    }\n\n    protected override async Task OnSpellPlay"),
    ("Spell/Todo/Equip/Cestus_of_Dagla.cs", "Cestus_of_Dagla",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Healing", "Uncommon",
     "    }\n\n    public override bool CanEquipTo"),
    ("Spell/Todo/Normal/Dark_Hole.cs", "Dark_Hole",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Burn", "Uncommon",
     "    }\n\n    protected override async Task OnSpellPlay"),
    ("Trap/Todo/Continuos/Des_Counterblow.cs", "Des_Counterblow",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn", "Uncommon",
     "    }\n\n    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;"),
    ("Spell/Todo/Normal/Emergency_Provisions.cs", "Emergency_Provisions",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Healing", "Uncommon",
     "    }\n\n    protected override bool IsPlayable =>"),
    ("Trap/Todo/Continuos/Fairy_Box.cs", "Fairy_Box",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn | YgoCardPackTags.Chance", "Uncommon",
     "    }\n\n    protected override async Task OnTrapPlay"),
    ("Spell/Todo/Normal/Polymerization.cs", "Polymerization",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Fusion", "Uncommon",
     "    }\n\n    protected override void OnUpgrade"),
    ("Trap/Todo/Normal/Rope_of_Life.cs", "Rope_of_Life",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap", "Uncommon",
     "    }\n\n    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>"),
    ("Spell/Todo/Normal/Rush_Recklessly.cs", "Rush_Recklessly",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", "Uncommon",
     "    }\n\n    protected override bool IsPlayable =>"),
    ("Spell/Todo/Normal/The_Reliable_Guardian.cs", "The_Reliable_Guardian",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", "Uncommon",
     "    }\n\n    protected override bool IsPlayable =>"),
    ("Trap/Todo/Continuos/Spellbinding_Circle.cs", "Spellbinding_Circle",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn", "Uncommon",
     "    }\n\n    protected override async Task OnTrapPlay"),
    ("Trap/Todo/Normal/Skull_Dice.cs", "Skull_Dice",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Chance", "Uncommon",
     "    }\n\n    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>"),
    ("Spell/Todo/Normal/Reinforcement_of_the_Army.cs", "Reinforcement_of_the_Army",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", "Uncommon",
     "    }\n\n    protected override Task OnSpellPlay"),
    ("Spell/Todo/Normal/Cost_Down.cs", "Cost_Down",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell", "Uncommon",
     "    }\n\n    protected override Task OnSpellPlay"),
    ("Trap/Todo/Normal/Compulsory_Evacuation_Device.cs", "Compulsory_Evacuation_Device",
     "YgoCardPackTags.Starter | YgoCardPackTags.Trap", "Uncommon",
     "    }\n\n    protected override async Task OnTrapPlay"),
    ("Spell/Todo/Normal/Book_of_Moon.cs", "Book_of_Moon",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell", "Uncommon",
     "    }\n\n    protected override async Task OnSpellPlay"),
    ("Spell/Todo/Equip/Gravity_Axe_Grarl.cs", "Gravity_Axe_Grarl",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell", "Uncommon",
     "    }\n\n    public override bool CanEquipTo"),
    ("Monster/Todo/Normal/Millennium_Shield.cs", "Millennium_Shield",
     "YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Warrior | YgoCardPackTags.Normal", "Uncommon",
     "__MILLENNIUM__"),
    ("Monster/Todo/Effect/Big_Shield_Gardna.cs", "Big_Shield_Gardna",
     "YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Warrior | YgoCardPackTags.Normal", "Uncommon",
     "__BIG_SHIELD__"),
    ("Monster/Todo/Effect/Terrorking_Archfiend.cs", "Terrorking_Archfiend",
     "YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Normal", "Uncommon",
     "    }\n\n    public override int PermanentAtkDeltaOnEnemyExecute"),
    ("Spell/Todo/Continuos/Convulsion_of_Nature.cs", "Convulsion_of_Nature",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", "Uncommon",
     "    }\n\n    public override StatEffectTotal GetContinuousStatEffect"),
    ("Spell/Todo/Continuos/Dark_Snake_Syndrome.cs", "Dark_Snake_Syndrome",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Burn", "Rare",
     "    }\n\n    public override StatEffectTotal GetContinuousStatEffect"),
    ("Spell/Pot_Of_Greed.cs", "Pot_Of_Greed",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", "Rare",
     "    }\n\n    protected override async Task OnSpellPlay"),
    ("Monster/Todo/Effect/Zombyra_the_Dark.cs", "Zombyra_the_Dark",
     "YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Burn | YgoCardPackTags.Normal", None,
     "    }\n\n    public override int PermanentAtkDeltaOnEnemyExecute"),
    ("Spell/Monster_Reborn.cs", "Monster_Reborn",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell", None,
     "    }\n\n    protected override void OnUpgrade"),
    ("Spell/Foolish_Burial.cs", "Foolish_Burial",
     "YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw_Search", None,
     "    }\n\n    protected override void OnUpgrade"),
]


def patch_millennium(text: str, cls: str, tags: str) -> str:
    old = """    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth |
        YgoCardPackTags.Warrior |
        YgoCardPackTags.Normal;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};


    // You will see these related cards more often with this card in your deck or side deck.
    //public override Type[] RelatedCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};
"""
    return text.replace(old, block_for(cls, tags))


def patch_big_shield(text: str, cls: str, tags: str) -> str:
    old = """    {
    }
}
"""
    new = """    {
    }

""" + block_for(cls, tags) + "}\n"
    return text.replace(old, new)


def patch_enraged(text: str) -> str:
    return text.replace(
        "public override YgoCardPackTags PackTags => YgoCardPackTags.Earth | YgoCardPackTags.Draw_Search | YgoCardPackTags.Normal;",
        "public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Draw_Search | YgoCardPackTags.Normal;",
    )


def main():
    n = 0
    for rel, cls, tags, rarity, marker in DATA:
        path = ROOT / rel
        text = path.read_text(encoding="utf-8")
        if "public override YgoCardPackTags PackTags" in text:
            print("already has PackTags, skip:", rel)
            continue
        text = ensure_usings(text)
        if rarity:
            text = apply_rarity(text, rarity)
        if marker == "__MILLENNIUM__":
            text = patch_millennium(text, cls, tags)
        elif marker == "__BIG_SHIELD__":
            text = patch_big_shield(text, cls, tags)
        else:
            ins = block_for(cls, tags)
            if marker not in text:
                raise SystemExit(f"marker missing {rel}: {marker[:50]!r}")
            text = text.replace(marker, "\n" + ins + marker.lstrip("\n"), 1)
        path.write_text(text, encoding="utf-8")
        n += 1

    enp = ROOT / "Monster/Enraged_Muka_Muka.cs"
    t = enp.read_text(encoding="utf-8")
    t2 = patch_enraged(ensure_usings(t))
    if t2 != t:
        enp.write_text(t2, encoding="utf-8")
        n += 1

    print("patched", n)


if __name__ == "__main__":
    main()
