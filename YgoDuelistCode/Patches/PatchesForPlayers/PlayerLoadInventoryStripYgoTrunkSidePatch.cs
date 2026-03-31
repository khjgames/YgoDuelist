using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Strips extra/trunk/side trailer from <see cref="SerializablePlayer.Deck"/> before <see cref="Player.PopulateDeck"/>; restores piles after load.
/// </summary>
[HarmonyPatch(typeof(Player), "LoadInventory")]
public static class PlayerLoadInventoryStripYgoTrunkSidePatch
{
    private static readonly Dictionary<ulong, YgoTrunkSideDeckLoadPending> DeferredLoadByPlayerNetId = new();

    private static bool CanLoadFromRunState(Player player)
    {
        return player.RunState is not null and not NullRunState;
    }

    internal static bool TryConsumeDeferred(Player player, out YgoTrunkSideDeckLoadPending pending)
    {
        lock (DeferredLoadByPlayerNetId)
        {
            return DeferredLoadByPlayerNetId.Remove(player.NetId, out pending!);
        }
    }

    private static void RestoreFromPending(Player player, YgoTrunkSideDeckLoadPending pending, string sourceTag)
    {
        GD.Print(
            $"[YgoDuelist][SaveLoad] RestorePending {sourceTag} netId={player.NetId} " +
            $"extra={pending.Extra.Count} trunk={pending.Trunk.Count} side={pending.Side.Count} " +
            $"minDeck={pending.LoadedMinimumDeckSize} owedRare={pending.LoadedOwedRareCardVouchers}");
        foreach (SerializableCard sc in pending.Extra)
        {
            CardModel card = player.RunState.LoadCard(sc, player);
            PlayerRunExtraDeck.GetOrCreatePile(player).AddInternal(card, -1, silent: true);
        }

        foreach (SerializableCard sc in pending.Trunk)
        {
            CardModel card = player.RunState.LoadCard(sc, player);
            PlayerRunTrunk.GetOrCreatePile(player).AddInternal(card, -1, silent: true);
        }

        foreach (SerializableCard sc in pending.Side)
        {
            CardModel card = player.RunState.LoadCard(sc, player);
            PlayerRunSideDeck.GetOrCreatePile(player).AddInternal(card, -1, silent: true);
        }

        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
        YgoPlayerMinimumDeck.SetLoadedFromSave(player, pending.LoadedMinimumDeckSize);
        YgoPackRewardProgress.SetOwedRareLoadedFromSave(player, pending.LoadedOwedRareCardVouchers);
    }

    public static void Prefix(Player __instance, SerializablePlayer save, ref object? __state)
    {
        __state = null;
        if (__instance.Character is not YgoChar)
            return;

        List<SerializableCard> deck = save.Deck;
        if (deck.Count == 0)
            return;

        ModelId markerId = ModelDb.Card<YgoSaveTrunkSideMarkerCard>().Id;
        SerializableCard last = deck[^1];
        if (!YgoSaveTrunkSideMarkerCard.IsMarker(last, markerId))
            return;

        YgoSaveTrunkSideMarkerCard.ReadTrailerCounts(last, out int extraCount, out int trunkCount, out int sideCount);
        int loadedMinDeck = YgoSaveTrunkSideMarkerCard.ReadMinimumDeckSizeOrDefault(
            last,
            YgoPlayerMinimumDeck.StartingMinimum);
        int loadedOwedRare = YgoSaveTrunkSideMarkerCard.ReadOwedRareCardVouchersOrDefault(last);

        int need = 1 + extraCount + trunkCount + sideCount;
        if (deck.Count < need)
        {
            GD.Print(
                $"[YgoDuelist][SaveLoad] LoadInventory trailer rejected netId={__instance.NetId} " +
                $"deckCount={deck.Count} need={need} extra={extraCount} trunk={trunkCount} side={sideCount}");
            return;
        }

        deck.RemoveAt(deck.Count - 1);

        var pending = new YgoTrunkSideDeckLoadPending
        {
            LoadedMinimumDeckSize = loadedMinDeck,
            LoadedOwedRareCardVouchers = loadedOwedRare
        };
        for (int i = 0; i < sideCount; i++)
        {
            pending.Side.Insert(0, deck[^1]);
            deck.RemoveAt(deck.Count - 1);
        }

        for (int i = 0; i < trunkCount; i++)
        {
            pending.Trunk.Insert(0, deck[^1]);
            deck.RemoveAt(deck.Count - 1);
        }

        for (int i = 0; i < extraCount; i++)
        {
            pending.Extra.Insert(0, deck[^1]);
            deck.RemoveAt(deck.Count - 1);
        }

        __state = pending;
        GD.Print(
            $"[YgoDuelist][SaveLoad] LoadInventory trailer parsed netId={__instance.NetId} " +
            $"extra={extraCount} trunk={trunkCount} side={sideCount} " +
            $"minDeck={loadedMinDeck} owedRare={loadedOwedRare} remainingMainDeck={deck.Count}");
    }

    public static void Postfix(Player __instance, object? __state)
    {
        if (__state is not YgoTrunkSideDeckLoadPending pending)
            return;

        if (CanLoadFromRunState(__instance))
        {
            RestoreFromPending(__instance, pending, "LoadInventoryPostfixImmediate");
            return;
        }

        lock (DeferredLoadByPlayerNetId)
        {
            DeferredLoadByPlayerNetId[__instance.NetId] = pending;
        }
        GD.Print(
            $"[YgoDuelist][SaveLoad] Deferred trailer restore due to NullRunState netId={__instance.NetId} " +
            $"extra={pending.Extra.Count} trunk={pending.Trunk.Count} side={pending.Side.Count}");
    }
}
