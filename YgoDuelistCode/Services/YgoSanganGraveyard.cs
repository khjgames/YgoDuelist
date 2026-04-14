using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Sangan"/>: sent from the field to the Graveyard — add 1 monster with printed ATK 15 or less from the deck to the hand
/// (name cannot be played for the rest of the turn).
/// </summary>
public static class YgoSanganGraveyard
{
    private static readonly LocString SearchPrompt = new("cards", "YGODUELIST-SANGAN.search_deck");

    public static async Task OnSentFromFieldToGraveyardAsync(Player player, Sangan sangan)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? hand = PileType.Hand.GetPile(player);
        if (draw == null || hand == null)
            return;

        List<BaseMonsterCard> candidates = draw.Cards
            .OfType<BaseMonsterCard>()
            .Where(m => m.BaseAtk <= 15)
            .ToList();
        if (candidates.Count == 0)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        var prefs = new CardSelectorPrefs(SearchPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> pick = await CardSelectCmd.FromSimpleGrid(ctx, candidates, player, prefs);
        if (pick.FirstOrDefault() is not BaseMonsterCard chosen)
            return;

        await CardPileCmd.Add(new CardModel[] { chosen }, hand, CardPilePosition.Top, chosen, false);
        YgoSanganNameLock.Set(player, chosen.Id.Entry);
    }
}
