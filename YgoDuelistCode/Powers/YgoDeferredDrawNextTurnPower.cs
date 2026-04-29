using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Counter stacks = extra cards to draw at the start of your next turn; then this power is removed.
/// Used by <see cref="YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Fenrir"/> (not <see cref="SuperRejuvenationPower"/>).
/// </summary>
public sealed class YgoDeferredDrawNextTurnPower : YgoDuelistPower
{
    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-YGO_DEFERRED_DRAW_NEXT_TURN_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-YGO_DEFERRED_DRAW_NEXT_TURN_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-YGO_DEFERRED_DRAW_NEXT_TURN_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "fenrir";

    private bool _pendingDraw;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _pendingDraw = true;
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || !_pendingDraw)
            return;

        _pendingDraw = false;

        int n = (int)Amount;
        if (n > 0)
            await CardPileCmd.Draw(choiceContext, n, player);

        await PowerCmd.Remove(this);
    }
}
