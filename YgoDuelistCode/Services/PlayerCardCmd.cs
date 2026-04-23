using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class PlayerCardCmd
{
    public static async Task DestroyAllAndDraw(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cardsToDestroy, int cardsToDraw)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        List<CardModel> destroyCards = cardsToDestroy.ToList();
        if (destroyCards.Count == 0)
        {
            return;
        }

        CardPile graveyardPile = YgoPlayerPiles.Graveyard(destroyCards[0].Owner);
        await CardPileCmd.Add(destroyCards, graveyardPile, CardPilePosition.Top, destroyCards[0], false);

        if (cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, cardsToDraw, destroyCards[0].Owner);
        }

    }

}
