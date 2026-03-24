using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Battle damage shield approximation while <see cref="Cards.Trap.Todo.Continuos.Tornado_Wall"/> is active.</summary>
public sealed class TornadoWallFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-TORNADO_WALL_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-TORNADO_WALL_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await CreatureCmd.GainBlock(Owner, 8m, default, null);
    }
}
