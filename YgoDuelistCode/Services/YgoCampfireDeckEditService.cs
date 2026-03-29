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
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Replace / add / remove flows for bonus campfire YGO deck edits (max deck = min(2× minimum, 99)).
/// </summary>
public static class YgoCampfireDeckEditService
{
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
                static c => c is YgoDuelistCard)).ToList();
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

        await CardPileCmd.RemoveFromDeck(picked[0]);
        YgoPlayerMinimumDeck.DecreaseAfterVoluntaryRemovals(player, 1);
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

        (List<CardModel> grid, List<CardModel> spawnedCatalog) = BuildPickGrid(player);
        if (grid.Count == 0)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        CardModel? chosen = null;
        bool catalogPickCommitted = false;
        try
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

            DetachFromTrunkOrSideIfNeeded(player, chosen);
            await CardPileCmd.Add(chosen, PileType.Deck);
            TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
            catalogPickCommitted = true;
        }
        finally
        {
            CleanupSpawnedCatalog(player, spawnedCatalog, chosen, catalogPickCommitted);
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
                static c => c is YgoDuelistCard)).ToList();
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

        (List<CardModel> grid, List<CardModel> spawnedCatalog) = BuildPickGrid(player);
        if (grid.Count == 0)
        {
            YgoCampfireDeckEditCharges.RefundOne(player);
            return;
        }

        CardModel? newCard = null;
        bool catalogPickCommitted = false;
        try
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
            await CardPileCmd.RemoveFromDeck(oldCard);
            DetachFromTrunkOrSideIfNeeded(player, newCard);
            await CardPileCmd.Add(newCard, PileType.Deck);
            TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
            catalogPickCommitted = true;
        }
        finally
        {
            CleanupSpawnedCatalog(player, spawnedCatalog, newCard, catalogPickCommitted);
        }
    }

    private static (List<CardModel> grid, List<CardModel> spawnedCatalog) BuildPickGrid(Player player)
    {
        var grid = new List<CardModel>();
        var spawnedCatalog = new List<CardModel>();

        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        foreach (CardModel c in trunk.Cards)
        {
            if (c is YgoDuelistCard)
                grid.Add(c);
        }

        foreach (CardModel c in side.Cards)
        {
            if (c is YgoDuelistCard)
                grid.Add(c);
        }

        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(static c => c.Id)
            .ToHashSet();

        foreach (CardModel template in YgoPackCardCatalog.GetAllYgoTemplates())
        {
            if (!unlocked.Contains(template.Id))
                continue;

            CardModel instance = player.RunState.CreateCard(template, player);
            spawnedCatalog.Add(instance);
            grid.Add(instance);
        }

        return (grid, spawnedCatalog);
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

    private static void CleanupSpawnedCatalog(
        Player player,
        List<CardModel> spawnedCatalog,
        CardModel? pickedFromCatalog,
        bool pickedWasCommittedToDeck)
    {
        foreach (CardModel c in spawnedCatalog)
        {
            if (pickedWasCommittedToDeck
                && pickedFromCatalog != null
                && ReferenceEquals(c, pickedFromCatalog))
            {
                continue;
            }

            player.RunState.RemoveCard(c);
        }
    }
}
