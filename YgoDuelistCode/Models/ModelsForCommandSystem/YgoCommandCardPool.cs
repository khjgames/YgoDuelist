using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Dedicated pool that owns all YgoDuelist command cards so that
/// CardModel.VisualCardPool never falls back to MockCardPool when
/// rendering them in UIs like NCardGrid.
///
/// This is a shared pool (not tied to a character) that is NOT used
/// for rewards or deckbuilding; it simply registers these cards
/// with ModelDb so UIs can render them safely.
/// </summary>
public partial class YgoCommandCardPool : CustomCardPoolModel
{
    public override string Title => "YGO_COMMAND";

    // Use base game ui_atlas sprite (card/energy_ironclad); no custom energy_ygo_command in atlas
    public override string EnergyColorName => "regent";

    // Slightly neutral grey; these cards shouldn't normally be visible,
    // but this keeps the pool definition consistent with Oddmelt.
    public override float H => 0.0f;
    public override float S => 0.0f;
    public override float V => 0.6f;

    public override bool IsShared => true;

    // All canonical command cards that should belong to this pool.
    public override Color DeckEntryCardColor => default;

    public override IEnumerable<CardModel> AllCards => new MonsterCommandCard[]
    {
        ModelDb.Card<Exit_Monster_Options>(),
        ModelDb.Card<Toggle_Die_For_You>(),
        ModelDb.Card<Command_Change_Battle_Position>(),
        ModelDb.Card<Command_Attack>(),
        ModelDb.Card<Command_Defend>(),
        ModelDb.Card<Activate_Effect>(),
        ModelDb.Card<Activate_Shackles>(),
        ModelDb.Card<Activate_Shackles_Plus>(),
        ModelDb.Card<Special_Summon_Egyptian_God_Slime>(),
        ModelDb.Card<Special_Summon_XY_Dragon_Cannon>(),
        ModelDb.Card<Special_Summon_XZ_Tank_Cannon>(),
        ModelDb.Card<Special_Summon_YZ_Tank_Dragon>(),
        ModelDb.Card<Special_Summon_VW_Tiger_Catapult>(),
        ModelDb.Card<Special_Summon_XYZ_Dragon_Cannon>(),
        ModelDb.Card<Special_Summon_VWXYZ_Dragon_Catapult_Cannon>(),
        ModelDb.Card<Heads>(),
        ModelDb.Card<PreviewEffect>(),
        ModelDb.Card<Tails>(),
        ModelDb.Card<Rolled_1>(),
        ModelDb.Card<Rolled_2>(),
        ModelDb.Card<Rolled_3>(),
        ModelDb.Card<Rolled_4>(),
        ModelDb.Card<Rolled_5>(),
        ModelDb.Card<Rolled_6>(),
        ModelDb.Card<Mausoleum_Lose_HP>(),
        ModelDb.Card<Fairy_Box_Upkeep_Take_Damage>(),
        ModelDb.Card<Fairy_Box_Upkeep_Destroy>(),
        ModelDb.Card<YgoTransientSpellOptionCommandCard>(),
    };

    public override bool IsColorless => false;
}
