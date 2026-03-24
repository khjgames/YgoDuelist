using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Turn-limited block shield; stacks count down each player turn.</summary>
public sealed class DustBarrierFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DUST_BARRIER_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DUST_BARRIER_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await CreatureCmd.GainBlock(Owner, 6m, default, null);
        await PowerCmd.ModifyAmount(this, -1m, null, null);
        if (Amount <= 0)
            await PowerCmd.Remove(this);
    }
}
