using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>At the start of your next turn, draw 1 additional card (then remove).</summary>
public sealed class DdDesignatorBonusDrawPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-DD_DESIGNATOR_BONUS_DRAW_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DD_DESIGNATOR_BONUS_DRAW_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;
        await CardPileCmd.Draw(choiceContext, 1, player);
        await PowerCmd.Remove(this);
    }
}
