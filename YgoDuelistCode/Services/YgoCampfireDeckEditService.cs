using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Campfire deck edits: move 1–3 YGO cards from deck to trunk, or from the side deck into the deck (max deck = min(2× minimum, 99)).
/// </summary>
public static class YgoCampfireDeckEditService
{
    private static bool IsCampfireDeckEditEligible(CardModel c) =>
        c is BaseMonsterCard or BaseSpellCard or BaseTrapCard;

    public static int GetDeckCap(Player player) =>
        Math.Min(YgoPlayerMinimumDeck.Get(player) * 2, 99);

    /// <summary>
    /// How many cards can still be removed from the deck without going below the run minimum (same cap as the store-to-trunk picker).
    /// </summary>
    public static int GetMaxRemovableFromDeck(Player player)
    {
        int minDeck = YgoPlayerMinimumDeck.Get(player);
        return Math.Max(0, player.Deck.Cards.Count - minDeck);
    }

    public static async Task RunStoreToTrunkAsync(Player player)
    {
        YgoCampfireDeckEditCharges.EnsureInitializedForRestSiteUi(player);
        int minDeck = YgoPlayerMinimumDeck.Get(player);
        int deckCount = player.Deck.Cards.Count;
        int storeRem = YgoCampfireDeckEditCharges.GetStoreRemaining(player);
        int maxRemovable = GetMaxRemovableFromDeck(player);

        GD.Print(
            $"[YgoDuelist][DeckToTrunkMin] StoreToTrunk requested player={player.NetId} deck={deckCount} minDeck={minDeck} storeCharges={storeRem} maxRemovable={maxRemovable}");

        if (player.Deck.Cards.Count <= minDeck)
        {
            GD.Print(
                $"[YgoDuelist][DeckToTrunkMin] StoreToTrunk blocked at min player={player.NetId} deck={deckCount} minDeck={minDeck}");
            return;
        }

        int maxPick = Math.Min(Math.Min(3, storeRem), maxRemovable);
        if (maxPick < 1)
        {
            GD.Print(
                $"[YgoDuelist][DeckToTrunkMin] StoreToTrunk blocked no capacity player={player.NetId} deck={deckCount} minDeck={minDeck} storeCharges={storeRem} maxRemovable={maxRemovable}");
            return;
        }

        var prefs = new CardSelectorPrefs(
            new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_STORE.prompt"),
            1,
            maxPick)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> picked;
        try
        {
            picked = (await CardSelectCmd.FromDeckForRemoval(
                player,
                prefs,
                IsCampfireDeckEditEligible)).ToList();
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (picked.Count == 0)
            return;

        GD.Print(
            $"[YgoDuelist][DeckToTrunkMin] StoreToTrunk moving player={player.NetId} picked={picked.Count} deckBefore={player.Deck.Cards.Count} minDeck={minDeck}");

        foreach (CardModel card in picked)
        {
            await CardPileCmd.RemoveFromDeck(card);
            YgoPlayerRunPiles.Trunk(player)?.AddInternal(card, -1, silent: true);
        }

        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
        YgoCampfireDeckEditCharges.ConsumeStore(player, picked.Count);
    }

    public static async Task RunPutInDeckAsync(Player player)
    {
        YgoCampfireDeckEditCharges.EnsureInitializedForRestSiteUi(player);
        int putRem = YgoCampfireDeckEditCharges.GetPutInDeckRemaining(player);
        int deckRoom = GetDeckCap(player) - player.Deck.Cards.Count;
        int maxPick = Math.Min(Math.Min(3, putRem), deckRoom);
        if (maxPick < 1)
            return;

        List<CardModel> grid = BuildPickGrid(player);
        if (grid.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(
            new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_PUT.prompt"),
            1,
            maxPick)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> pick = await YgoSimpleGridSelection.TrySelectAsync(player, grid, prefs);
        if (pick.Count == 0)
            return;

        int added = 0;
        foreach (CardModel chosen in pick)
        {
            if (player.Deck.Cards.Count >= GetDeckCap(player))
                break;
            if (!IsCampfireDeckEditEligible(chosen))
                break;

            DetachFromTrunkOrSideIfNeeded(player, chosen);
            await CardPileCmd.Add(chosen, PileType.Deck, source: chosen);
            added++;
        }

        if (added > 0)
        {
            TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
            YgoCampfireDeckEditCharges.ConsumePutInDeck(player, added);
        }
    }

    private static List<CardModel> BuildPickGrid(Player player)
    {
        var grid = new List<CardModel>();
        CardPile? side = YgoPlayerRunPiles.SideDeck(player);
        if (side == null)
            return grid;
        foreach (CardModel c in side.Cards)
        {
            if (IsCampfireDeckEditEligible(c))
                grid.Add(c);
        }

        return grid;
    }

    private static void DetachFromTrunkOrSideIfNeeded(Player player, CardModel card)
    {
        CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
        if (trunk != null && trunk.Cards.Contains(card))
        {
            trunk.RemoveInternal(card, silent: true);
            return;
        }

        CardPile? side = YgoPlayerRunPiles.SideDeck(player);
        if (side != null && side.Cards.Contains(card))
            side.RemoveInternal(card, silent: true);
    }
}
