using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Monsters in your hand are treated as 2 Levels lower (this turn).</summary>
public sealed class CostDownHandLevelPower : YgoDuelistPower
{
    public const int LevelReduction = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-COST_DOWN_HAND_LEVEL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-COST_DOWN_HAND_LEVEL_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;
        await PowerCmd.Remove(this);
    }
}
