# One-off: batch 5 (inventory entries 401-449) — cards.json descriptions + trap C# CanonicalVars.
# Run from repo root: python tools/apply_batch5_inventory.py
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS_JSON = ROOT / "YgoDuelist" / "localization" / "eng" / "cards.json"
TRAP_ROOT = ROOT / "YgoDuelistCode" / "Cards" / "Trap" / "Todo"

# (subfolder, ClassName, DuelMonsterRace suffix, cost, desc_key_suffix, description)
# DuelMonsterRace: TrapContinuous, TrapNormal, TrapCounter
ROWS = [
    ("Continuos", "Narrow_Pass", "TrapContinuous", 1, "NARROW_PASS",
     "This card can only be activated when both players have 2 monsters or less on their respective sides of the field. Both players can only Normal Summon up to 2 additional monsters on their sides of the field."),
    ("Normal", "Negate_Attack", "TrapCounter", 1, "NEGATE_ATTACK",
     "When an opponent's monster declares an attack: Negate that attack, then end the Battle Phase."),
    ("Continuos", "Nightmare_Wheel", "TrapContinuous", 1, "NIGHTMARE_WHEEL",
     "Activate by targeting 1 face-up monster your opponent controls. Each of your Standby Phases, it takes [blue]{Mgc}[/blue] damage. If that monster is destroyed, destroy this card."),
    ("Continuos", "Ninjitsu_Art_of_Transformation", "TrapContinuous", 1, "NINJITSU_ART_OF_TRANSFORMATION",
     "Tribute 1 face-up \"Ninja\" monster you control; Special Summon 1 Beast, Insect, or Plant from your hand in Defense Position, ignoring its Summoning conditions."),
    ("Normal", "Numinous_Healer", "TrapNormal", 1, "NUMINOUS_HEALER",
     "When your HP would drop below [blue]{Mgc}[/blue], gain [blue]{Mgc2}[/blue] HP instead (once per combat)."),
    ("Normal", "Nutrient_Z", "TrapNormal", 1, "NUTRIENT_Z",
     "When a monster you control is attacked by a monster with higher ATK: Gain HP equal to half the ATK difference (max [blue]{Mgc}[/blue])."),
    ("Normal", "Ojama_Trio", "TrapNormal", 1, "OJAMA_TRIO",
     "Special Summon [blue]{Mgc}[/blue] \"Ojama\" tokens to your opponent's field (their Monster Zones)."),
    ("Continuos", "Ominous_Fortunetelling", "TrapContinuous", 1, "OMINOUS_FORTUNETELLING",
     "Once per turn, during your Standby Phase: Name Spell, Trap, or Monster; reveal the top card of your draw pile — if it matches, draw [blue]{Mgc}[/blue] card(s)."),
    ("Continuos", "Ordeal_of_a_Traveler", "TrapContinuous", 1, "ORDEAL_OF_A_TRAVELER",
     "Once per attack declaration, your opponent guesses a card type in your hand; if wrong, they take [blue]{Mgc}[/blue] damage."),
    ("Normal", "Order_to_Smash", "TrapNormal", 1, "ORDER_TO_SMASH",
     "Tribute 1 monster you control, then destroy 1 face-down card on the field."),
    ("Normal", "Pharaoh_s_Treasure", "TrapNormal", 1, "PHARAOH_S_TREASURE",
     "Each time you gain Spell Counters, place [blue]{Mgc}[/blue] additional Spell Counter(s) on this card."),
    ("Normal", "Physical_Double", "TrapNormal", 1, "PHYSICAL_DOUBLE",
     "When your monster is attacked: That monster's ATK becomes double its original ATK during damage calculation (this turn)."),
    ("Continuos", "Pitch_Black_Power_Stone", "TrapContinuous", 1, "PITCH_BLACK_POWER_STONE",
     "When this card is activated, place [blue]{Mgc}[/blue] Spell Counter(s) on it. Once per turn, during your Standby Phase, place 1 more Spell Counter on it."),
    ("Normal", "Pyro_Clock_of_Destiny", "TrapNormal", 1, "PYRO_CLOCK_OF_DESTINY",
     "Advance the turn counter by [blue]{Mgc}[/blue] (for effects that care about the turn number)."),
    ("Normal", "Raigeki_Break", "TrapNormal", 1, "RAIGEKI_BREAK",
     "Discard 1 card, then destroy 1 card on the field."),
    ("Normal", "Ray_of_Hope", "TrapNormal", 1, "RAY_OF_HOPE",
     "If you have at least [blue]{Mgc}[/blue] Spell(s) in your discard pile, add 1 LIGHT monster from your discard pile to your hand."),
    ("Normal", "Reckless_Greed", "TrapNormal", 1, "RECKLESS_GREED",
     "Draw [blue]{Mgc}[/blue] cards, then skip your next [blue]{Mgc}[/blue] draw(s)."),
    ("Normal", "Reinforcements", "TrapNormal", 1, "REINFORCEMENTS",
     "When a monster you control declares an attack: It gains [blue]{Mgc}[/blue] ATK during damage calculation only."),
    ("Normal", "Reverse_Trap", "TrapNormal", 1, "REVERSE_TRAP",
     "Until the End Phase, all increases and decreases to ATK/DEF are reversed."),
    ("Normal", "Ring_of_Destruction", "TrapNormal", 1, "RING_OF_DESTRUCTION",
     "Target 1 face-up monster on the field; destroy it, and both players take damage equal to its ATK (YGO-accurate timing simplified)."),
    ("Normal", "Rite_of_Spirit", "TrapNormal", 1, "RITE_OF_SPIRIT",
     "Discard 1 card; add 1 Level 3 or lower Normal Monster from your discard pile to your hand."),
    ("Continuos", "Rivalry_of_Warlords", "TrapContinuous", 1, "RIVALRY_OF_WARLORDS",
     "Each player can only control monsters of [blue]{Mgc}[/blue] Type (each player chooses 1 Type while this card is active)."),
    ("Continuos", "Robbin_Goblin", "TrapContinuous", 1, "ROBBIN_GOBLIN",
     "Each time you deal unblocked damage to an enemy, they discard [blue]{Mgc}[/blue] random card(s)."),
    ("Continuos", "Robbin_Zombie", "TrapContinuous", 1, "ROBBIN_ZOMBIE",
     "Each time you deal unblocked damage to an enemy, they put [blue]{Mgc}[/blue] card(s) from the top of their draw pile into their discard pile."),
    ("Normal", "Rope_of_Life", "TrapNormal", 1, "ROPE_OF_LIFE",
     "When a monster you control is destroyed: Discard your entire hand, then Special Summon that monster with [blue]{Mgc}[/blue] additional ATK."),
    ("Normal", "Sakuretsu_Armor", "TrapNormal", 1, "SAKURETSU_ARMOR",
     "When an opponent's monster declares an attack: Destroy the attacking monster."),
    ("Normal", "Secret_Barrel", "TrapNormal", 1, "SECRET_BARREL",
     "Inflict [blue]{Mgc}[/blue] damage to a random enemy for each card in the enemy's hand."),
    ("Normal", "Self_Destruct_Button", "TrapNormal", 1, "SELF_DESTRUCT_BUTTON",
     "When your HP reaches 0: Set both players' HP to [blue]{Mgc}[/blue] (once per run / simplified)."),
    ("Normal", "Skull_Dice", "TrapNormal", 1, "SKULL_DICE",
     "Roll a six-sided die; all enemies lose [blue]{Mgc}[/blue] Strength this turn (result × this value, capped)."),
    ("Continuos", "Skull_Invitation", "TrapContinuous", 1, "SKULL_INVITATION",
     "Each time a card(s) is sent to the discard pile, inflict [blue]{Mgc}[/blue] damage to that card's owner for each card sent."),
    ("Continuos", "Skull_Lair", "TrapContinuous", 1, "SKULL_LAIR",
     "Remove from play [blue]{Mgc}[/blue] monster(s) from your discard pile; destroy 1 face-up monster on the field."),
    ("Normal", "Solar_Ray", "TrapNormal", 1, "SOLAR_RAY",
     "If you have more HP than each enemy: Destroy 1 face-up monster with ATK lower than [blue]{Mgc2}[/blue] (HP threshold [blue]{Mgc}[/blue])."),
    ("Normal", "Solemn_Judgment", "TrapCounter", 1, "SOLEMN_JUDGMENT",
     "When a monster would be Summoned or a Spell/Trap is activated: Pay half your HP; negate the Summon or activation and destroy that card."),
    ("Continuos", "Solemn_Wishes", "TrapContinuous", 1, "SOLEMN_WISHES",
     "Each time you activate a Spell Card, gain [blue]{Mgc}[/blue] HP."),
    ("Continuos", "Soul_Demolition", "TrapContinuous", 1, "SOUL_DEMOLITION",
     "Once per turn: Your opponent removes [blue]{Mgc}[/blue] card(s) from their discard pile from the game."),
    ("Continuos", "Soul_Resurrection", "TrapContinuous", 1, "SOUL_RESURRECTION",
     "Once per turn: Pay [blue]{Mgc}[/blue] HP; Special Summon 1 Normal Monster from your discard pile in Defense Position."),
    ("Normal", "Statue_of_the_Wicked", "TrapNormal", 1, "STATUE_OF_THE_WICKED",
     "When this Set card is destroyed and sent to the discard pile: Special Summon [blue]{Mgc}[/blue] \"Wicked Token(s)\"."),
    ("Continuos", "The_First_Monarch", "TrapContinuous", 1, "THE_FIRST_MONARCH",
     "Tribute 1 Level 5 or higher monster; Special Summon 1 Level 5 or higher monster from your hand with [blue]{Mgc}[/blue] fewer Star cost (simplified)."),
    ("Continuos", "The_First_Sarcophagus", "TrapContinuous", 1, "THE_FIRST_SARCOPHAGUS",
     "Each of your End Phases, place 1 piece counter on this card (max [blue]{Mgc}[/blue]). When you have 3, Special Summon \"Spirit of Pharaoh\"."),
    ("Normal", "The_Spell_Absorbing_Life", "TrapNormal", 1, "THE_SPELL_ABSORBING_LIFE",
     "Flip all face-down monsters face-up; destroy all non-Spellcaster monsters with 1500 or less DEF. Gain [blue]{Mgc}[/blue] HP for each destroyed."),
    ("Normal", "Time_Machine", "TrapNormal", 1, "TIME_MACHINE",
     "When a monster you control is destroyed by battle: Special Summon it, then it gains [blue]{Mgc}[/blue] ATK."),
    ("Continuos", "Tower_of_Babel", "TrapContinuous", 1, "TOWER_OF_BABEL",
     "Each time a Spell Card is activated, inflict [blue]{Mgc}[/blue] damage to its owner."),
    ("Normal", "Trap_Hole", "TrapNormal", 1, "TRAP_HOLE",
     "When your opponent Normal or Flip Summons a monster with [blue]{Mgc}[/blue] or fewer Stars: Destroy it."),
    ("Normal", "Trap_of_Board_Eraser", "TrapNormal", 1, "TRAP_OF_BOARD_ERASER",
     "When your opponent controls [blue]{Mgc}[/blue] or more Spell/Trap cards: They discard [blue]{Mgc2}[/blue] until they control 4."),
    ("Normal", "Waboku", "TrapNormal", 1, "WABOKU",
     "Until the End Phase, you take no damage from battles involving your monsters."),
    ("Normal", "White_Hole", "TrapNormal", 1, "WHITE_HOLE",
     "When \"Dark Hole\" resolves: Negate its effect on your monsters (simplified)."),
    ("Normal", "Widespread_Ruin", "TrapNormal", 1, "WIDESPREAD_RUIN",
     "When an opponent's monster declares an attack: Destroy the attacking monster with the highest ATK (if tied, you choose)."),
    ("Normal", "Windstorm_of_Etaqua", "TrapNormal", 1, "WINDSTORM_OF_ETAQUA",
     "Return up to [blue]{Mgc}[/blue] Spell/Trap cards your opponent controls to the hand."),
]

# (ClassName, vars: list of (name, base, upgrade_delta)), None = no CanonicalVars override
VARS = {
    "Nightmare_Wheel": [("Mgc", 5, 2)],
    "Numinous_Healer": [("Mgc", 40, -5), ("Mgc2", 20, 5)],
    "Nutrient_Z": [("Mgc", 15, 5)],
    "Ojama_Trio": [("Mgc", 3, 1)],
    "Ominous_Fortunetelling": [("Mgc", 1, 1)],
    "Ordeal_of_a_Traveler": [("Mgc", 8, 4)],
    "Pharaoh_s_Treasure": [("Mgc", 1, 1)],
    "Pitch_Black_Power_Stone": [("Mgc", 3, 1)],
    "Pyro_Clock_of_Destiny": [("Mgc", 1, 1)],
    "Ray_of_Hope": [("Mgc", 3, -1)],
    "Reckless_Greed": [("Mgc", 2, 1)],
    "Reinforcements": [("Mgc", 5, 2)],
    "Rivalry_of_Warlords": [("Mgc", 1, 0)],
    "Robbin_Goblin": [("Mgc", 1, 1)],
    "Robbin_Zombie": [("Mgc", 1, 1)],
    "Rope_of_Life": [("Mgc", 8, 4)],
    "Secret_Barrel": [("Mgc", 2, 1)],
    "Self_Destruct_Button": [("Mgc", 1, 1)],
    "Skull_Dice": [("Mgc", 1, 1)],
    "Skull_Invitation": [("Mgc", 3, 2)],
    "Skull_Lair": [("Mgc", 3, -1)],
    "Solar_Ray": [("Mgc", 20, 5), ("Mgc2", 15, 5)],
    "Solemn_Wishes": [("Mgc", 5, 3)],
    "Soul_Demolition": [("Mgc", 2, 1)],
    "Soul_Resurrection": [("Mgc", 5, -2)],
    "Statue_of_the_Wicked": [("Mgc", 1, 1)],
    "The_First_Monarch": [("Mgc", 1, 1)],
    "The_First_Sarcophagus": [("Mgc", 3, 1)],
    "The_Spell_Absorbing_Life": [("Mgc", 5, 3)],
    "Time_Machine": [("Mgc", 5, 3)],
    "Tower_of_Babel": [("Mgc", 3, 2)],
    "Trap_Hole": [("Mgc", 4, -1)],
    "Trap_of_Board_Eraser": [("Mgc", 6, -1), ("Mgc2", 2, 1)],
    "Windstorm_of_Etaqua": [("Mgc", 2, 1)],
}


def cs_body(subfolder: str, class_name: str, race: str, cost: int) -> str:
    ns = f"YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.{subfolder}"
    spec = VARS.get(class_name)
    if spec is None:
        return f"""using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace {ns};

public sealed class {class_name} : BaseTrapCard
{{
    public {class_name}()
        : base(cost: {cost}, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.{race})
    {{
    }}

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {{
    }}
}}
"""
    lines_vars = []
    for name, base, _ in spec:
        lines_vars.append(f'            new DynamicVar("{name}", {base}m),')
    vars_block = "\n".join(lines_vars)
    up_lines = []
    for name, _, delta in spec:
        if delta != 0:
            up_lines.append(f'        DynamicVars["{name}"].UpgradeValueBy({delta}m);')
    upgrade_body = "\n".join(up_lines)

    return f"""using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace {ns};

public sealed class {class_name} : BaseTrapCard
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {{
{vars_block}
        }};

    public {class_name}()
        : base(cost: {cost}, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.{race})
    {{
    }}

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {{
{upgrade_body}
    }}
}}
"""


def main():
    data = json.loads(CARDS_JSON.read_text(encoding="utf-8"))
    for sub, cls, race, cost, key_suffix, desc in ROWS:
        data[f"YGODUELIST-{key_suffix}.description"] = desc
    CARDS_JSON.write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    for sub, cls, race, cost, _, _ in ROWS:
        path = TRAP_ROOT / sub / f"{cls}.cs"
        path.write_text(cs_body(sub, cls, race, cost), encoding="utf-8")

    print("Updated cards.json and", len(ROWS), "trap .cs files.")


if __name__ == "__main__":
    main()
