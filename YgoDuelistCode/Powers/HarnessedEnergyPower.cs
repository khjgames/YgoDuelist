using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Draw 1 fewer card from your turn draw; at the start of each of your turns, gain Energy equal to stacks (1 from the spell).
/// </summary>
public sealed class HarnessedEnergyPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-HARNESSED_ENERGY_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-HARNESSED_ENERGY_POWER.description");

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.ForEnergy(this); }
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player)
            return count;
        return System.Math.Max(0m, count - 1m);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0m)
            return;
        await PlayerCmd.GainEnergy(Amount, player);
    }
}
