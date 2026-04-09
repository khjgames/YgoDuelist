using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// For every N cards you send to the Graveyard, draw 1 (N is 5, or 4 if upgraded).
/// </summary>
public sealed class FallenInsightsPower : YgoDuelistPower
{
    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-FALLEN_INSIGHTS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FALLEN_INSIGHTS_POWER.description");

    private int _threshold = 5;
    private int _progress;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _threshold = cardSource is { IsUpgraded: true } ? 4 : 5;
        _progress = 0;
        await base.AfterApplied(applier, cardSource);
    }

    public async Task OnOwnerCardsAddedToGraveyardAsync(PlayerChoiceContext choiceContext, int cardCount)
    {
        if (cardCount <= 0 || Owner?.Player == null)
            return;

        _progress += cardCount;
        while (_progress >= _threshold)
        {
            _progress -= _threshold;
            await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
        }
    }
}
