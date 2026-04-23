using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Field → GY: add one main-deck monster matching <see cref="IFieldToGraveyardDeckSearchEffect"/> to hand.</summary>
public static class YgoFieldToGraveyardDeckSearch
{
    public static async Task OnSentFromFieldToGraveyardAsync(Player player, BaseMonsterCard source)
    {
        if (source is not IFieldToGraveyardDeckSearchEffect fx)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (draw == null || hand == null)
            return;

        var ctx = YgoChoiceContexts.Blocking();
        var prefs = new CardSelectorPrefs(fx.FieldToGraveyardSearchPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<BaseMonsterCard> BuildCurrentSearchTargets() =>
            BuildSearchTargets(draw, fx.FieldToGraveyardSearchMaxPrintedAtk);

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            prefs,
            BuildCurrentSearchTargets);
        if (chosen == null)
            return;

        await CardPileCmd.Add(new CardModel[] { chosen }, hand, CardPilePosition.Top, chosen, false);
        if (fx.FieldToGraveyardSearchApplyNameLock)
            YgoSanganNameLock.Set(player, chosen.Id.Entry);
    }

    private static List<BaseMonsterCard> BuildSearchTargets(CardPile draw, int maxPrintedAtk) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(draw.Cards)
        .OfType<BaseMonsterCard>()
        .Where(m => m.BaseAtk <= maxPrintedAtk)
        .ToList();
}
