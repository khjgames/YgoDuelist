using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// End of the turn you played this: latch draw count (dragons destroyed that turn + upgrade bonus).
/// Start of your next turn: draw that many cards, then remove.
/// </summary>
public sealed class SuperRejuvenationPower : YgoDuelistPower
{
    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SUPER_REJUVENATION_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SUPER_REJUVENATION_POWER.description");

    protected override bool IsVisibleInternal => _latchedDrawCount;

    private int _upgradeDrawBonus;
    private bool _latchedDrawCount;
    private bool _awaitingDrawOnTurnStart;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (cardSource is { IsUpgraded: true })
            _upgradeDrawBonus = 1;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        Player? player = Owner.Player;
        if (player == null)
            return;

        if (_latchedDrawCount)
            return;

        _latchedDrawCount = true;

        GraveyardRelic? g = player.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        int dragons = g?.DragonMonstersDestroyedThisTurn ?? 0;
        int total = dragons + _upgradeDrawBonus;

        if (total <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        decimal delta = total - (decimal)Amount;
        await PowerCmd.ModifyAmount(this, delta, null, null);
        _awaitingDrawOnTurnStart = true;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || !_awaitingDrawOnTurnStart)
            return;

        _awaitingDrawOnTurnStart = false;

        int n = (int)Amount;
        if (n > 0)
            await CardPileCmd.Draw(choiceContext, n, player);

        await PowerCmd.Remove(this);
    }
}
