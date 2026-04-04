using System;
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
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Face-up <see cref="Card_Trader"/> in the Spell/Trap zone: start of turn hand → draw pile, then draw.</summary>
public static class YgoCardTraderContinuous
{
    private const string AnnualKey = "CARD_TRADER";

    public static async Task TryResolvePlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == null)
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return;

        int traders = zone.Cards.OfType<Card_Trader>().Count(c => !c.FaceDown);
        if (traders == 0)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null || hand.Cards.Count == 0)
            return;

        CardPile? drawPile = PileType.Draw.GetPile(player);
        if (drawPile == null)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
            return;

        int maxPick = Math.Min(traders, hand.Cards.Count);
        var candidates = hand.Cards.ToList();

        var prompt = new LocString("cards", "YGODUELIST-CARD_TRADER.turn_selection.title");
        prompt.Add("Max", maxPick);

        var prefs = new CardSelectorPrefs(prompt, 0, maxPick)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        IEnumerable<CardModel> picked = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, player, prefs);
        List<CardModel> toDeck = picked.Where(c => hand.Cards.Contains(c)).Distinct().ToList();
        if (toDeck.Count > maxPick)
            toDeck = toDeck.Take(maxPick).ToList();

        foreach (CardModel card in toDeck)
            await CardPileCmd.Add(new[] { card }, drawPile, CardPilePosition.Random, card, false);

        if (toDeck.Count > 0)
            await CardPileCmd.Draw(choiceContext, toDeck.Count, player);
    }
}
