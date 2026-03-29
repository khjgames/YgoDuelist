using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

public sealed class Draw1LessPerTurnPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DRAW1_LESS_PER_TURN_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DRAW1_LESS_PER_TURN_POWER.description");

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != base.Owner.Player || base.Amount <= 0)
        {
            return count;
        }
        return Math.Max(0m, count - 1m);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player || base.Amount <= 0)
        {
            return;
        }
        await PowerCmd.Decrement(this);
    }
}
