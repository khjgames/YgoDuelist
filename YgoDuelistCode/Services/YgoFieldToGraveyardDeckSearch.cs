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

        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? hand = PileType.Hand.GetPile(player);
        if (draw == null || hand == null)
            return;

        int cap = fx.FieldToGraveyardSearchMaxPrintedAtk;
        List<BaseMonsterCard> candidates = draw.Cards
            .OfType<BaseMonsterCard>()
            .Where(m => m.BaseAtk <= cap)
            .ToList();
        if (candidates.Count == 0)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        var prefs = new CardSelectorPrefs(fx.FieldToGraveyardSearchPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> pick = await CardSelectCmd.FromSimpleGrid(ctx, candidates, player, prefs);
        if (pick.FirstOrDefault() is not BaseMonsterCard chosen)
            return;

        await CardPileCmd.Add(new CardModel[] { chosen }, hand, CardPilePosition.Top, chosen, false);
        if (fx.FieldToGraveyardSearchApplyNameLock)
            YgoSanganNameLock.Set(player, chosen.Id.Entry);
    }
}
