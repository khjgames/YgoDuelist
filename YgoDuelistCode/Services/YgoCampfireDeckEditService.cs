using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Replace / add / remove flows for bonus campfire YGO deck edits (max deck = min(2× minimum, 99)).
/// </summary>
public static class YgoCampfireDeckEditService
{
    private static bool IsCampfireDeckEditEligible(CardModel c) =>
        c is BaseMonsterCard or BaseSpellCard or BaseTrapCard;

    public static int GetDeckCap(Player player) =>
        Math.Min(YgoPlayerMinimumDeck.Get(player) * 2, 99);

    public static async Task RunRemoveAsync(Player player)
    {
        if (!YgoCampfireDeckEditCharges.TryConsumeOne(player))
            return;

        var prefs = new CardSelectorPrefs(
            new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_REMOVE.prompt"),
            1)
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
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        if (picked.Count == 0)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        CardModel removed = picked[0];
        await CardPileCmd.RemoveFromDeck(removed);
        PlayerRunTrunk.GetOrCreatePile(player).AddInternal(removed, -1, silent: true);
        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
    }

    public static async Task RunAddAsync(Player player)
    {
        if (!YgoCampfireDeckEditCharges.TryConsumeOne(player))
            return;

        if (player.Deck.Cards.Count >= GetDeckCap(player))
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        List<CardModel> grid = BuildPickGrid(player);
        if (grid.Count == 0)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        CardModel? chosen = null;
        {
            var prefs = new CardSelectorPrefs(
                new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_ADD.prompt"),
                0,
                1)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            };

            List<CardModel> pick;
            try
            {
                pick = (await CardSelectCmd.FromSimpleGrid(
                    new BlockingPlayerChoiceContext(),
                    grid,
                    player,
                    prefs)).ToList();
            }
            catch (OperationCanceledException)
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }

            if (pick.Count == 0)
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }

            chosen = pick[0];
            if (player.Deck.Cards.Count >= GetDeckCap(player))
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }
            if (!IsCampfireDeckEditEligible(chosen))
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }

            DetachFromTrunkOrSideIfNeeded(player, chosen);
            // Non-null source: Bing Bong only duplicates when source is null; trunk/side use PileType.None + AddInternal so this add would otherwise read as a normal deck gain.
            await CardPileCmd.Add(chosen, PileType.Deck, source: chosen);
            TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
        }
    }

    public static async Task RunReplaceAsync(Player player)
    {
        if (!YgoCampfireDeckEditCharges.TryConsumeOne(player))
            return;

        var prefsOld = new CardSelectorPrefs(
            new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_REPLACE.pick_old"),
            1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> oldPick;
        try
        {
            oldPick = (await CardSelectCmd.FromDeckForRemoval(
                player,
                prefsOld,
                IsCampfireDeckEditEligible)).ToList();
        }
        catch (OperationCanceledException)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        if (oldPick.Count == 0)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        CardModel oldCard = oldPick[0];

        List<CardModel> grid = BuildPickGrid(player);
        if (grid.Count == 0)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        CardModel? newCard = null;
        {
            var prefsNew = new CardSelectorPrefs(
                new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_REPLACE.pick_new"),
                0,
                1)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            };

            List<CardModel> newPick;
            try
            {
                newPick = (await CardSelectCmd.FromSimpleGrid(
                    new BlockingPlayerChoiceContext(),
                    grid,
                    player,
                    prefsNew)).ToList();
            }
            catch (OperationCanceledException)
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }

            if (newPick.Count == 0)
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }

            newCard = newPick[0];
            if (!IsCampfireDeckEditEligible(newCard))
            {
                YgoCampfireDeckEditCharges.RefundOne(player);
                return;
            }
            YgoDeckRemovalMinTracker.SkipNextRunDeckRemovalMin(oldCard);
            await CardPileCmd.RemoveFromDeck(oldCard);
            DetachFromTrunkOrSideIfNeeded(player, newCard);
            await CardPileCmd.Add(newCard, PileType.Deck, source: newCard);
            TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
        }
    }

    private static List<CardModel> BuildPickGrid(Player player)
    {
        var grid = new List<CardModel>();

        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        foreach (CardModel c in trunk.Cards)
        {
            if (IsCampfireDeckEditEligible(c))
                grid.Add(c);
        }

        foreach (CardModel c in side.Cards)
        {
            if (IsCampfireDeckEditEligible(c))
                grid.Add(c);
        }
        return grid;
    }

    private static void DetachFromTrunkOrSideIfNeeded(Player player, CardModel card)
    {
        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        if (trunk.Cards.Contains(card))
        {
            trunk.RemoveInternal(card, silent: true);
            return;
        }

        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        if (side.Cards.Contains(card))
            side.RemoveInternal(card, silent: true);
    }

}
