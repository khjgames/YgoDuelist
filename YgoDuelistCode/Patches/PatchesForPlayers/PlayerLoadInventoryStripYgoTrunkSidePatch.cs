using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Mutates <paramref name="deck"/> in place: removes marker and trailing extra/trunk/side serial rows.
    /// Used by <see cref="Player.LoadInventory"/> and <see cref="Player.SyncWithSerializedPlayer"/> (multiplayer).
    /// </summary>
    internal static bool TryStripTrailerFromDeckList(
        List<SerializableCard> deck,
        ulong netId,
        string logPrefix,
        [NotNullWhen(true)] out YgoTrunkSideDeckLoadPending? pending)
    {
        pending = null;
        if (deck.Count == 0)
            return false;

        if (!YgoSerializableDeckLists.TryExtractTrailer(deck, out YgoSerializableDeckLists.SplitResult split))
            return false;

        SerializableCard marker = split.Marker;
        int extraCount = split.Extra.Count;
        int trunkCount = split.Trunk.Count;
        int sideCount = split.Side.Count;
        int loadedMinDeck = YgoSaveTrunkSideMarkerCard.ReadMinimumDeckSizeOrDefault(
            marker,
            YgoPlayerMinimumDeck.StartingMinimum);
        int loadedMinDeckReceivedCardProgress =
            YgoSaveTrunkSideMarkerCard.ReadMinimumDeckReceivedCardProgressOrDefault(marker);
        int loadedOwedRare = YgoSaveTrunkSideMarkerCard.ReadOwedRareCardVouchersOrDefault(marker);
        string? loadedPackTagBalance = YgoSaveTrunkSideMarkerCard.ReadPackTagBalanceOrNull(marker);

        var p = new YgoTrunkSideDeckLoadPending
        {
            LoadedMinimumDeckSize = loadedMinDeck,
            LoadedMinimumDeckReceivedCardProgress = loadedMinDeckReceivedCardProgress,
            LoadedOwedRareCardVouchers = loadedOwedRare,
            LoadedPackTagBalance = loadedPackTagBalance
        };
        p.Extra.AddRange(split.Extra);
        p.Trunk.AddRange(split.Trunk);
        p.Side.AddRange(split.Side);

        pending = p;
        GD.Print(
            $"{logPrefix} trailer parsed netId={netId} extra={extraCount} trunk={trunkCount} side={sideCount} " +
            $"minDeck={loadedMinDeck} minDeckReceivedProgress={loadedMinDeckReceivedCardProgress} owedRare={loadedOwedRare} remainingMainDeck={deck.Count}");
        return true;
    }

    /// <summary>Clears off-deck piles before re-applying a sync payload (avoids duplicate cards).</summary>
    internal static void ClearYgoOffDeckPiles(Player player)
    {
        YgoPlayerRunPiles.RunExtraDeckIfExists(player)?.Clear(silent: true);
        PlayerRunTrunk.GetPileIfExists(player)?.Clear(silent: true);
        PlayerRunSideDeck.GetPileIfExists(player)?.Clear(silent: true);
    }

    internal static void RestoreFromPending(Player player, YgoTrunkSideDeckLoadPending pending, string sourceTag)
    {
        GD.Print(
            $"[YgoDuelist][SaveLoad] RestorePending {sourceTag} netId={player.NetId} " +
            $"extra={pending.Extra.Count} trunk={pending.Trunk.Count} side={pending.Side.Count} " +
            $"minDeck={pending.LoadedMinimumDeckSize} minDeckReceivedProgress={pending.LoadedMinimumDeckReceivedCardProgress} owedRare={pending.LoadedOwedRareCardVouchers}");
        foreach (SerializableCard sc in pending.Extra)
        {
            CardModel card = player.RunState.LoadCard(sc, player);
            YgoPlayerRunPiles.RunExtraDeck(player)?.AddInternal(card, -1, silent: true);
        }

        foreach (SerializableCard sc in pending.Trunk)
        {
            CardModel card = player.RunState.LoadCard(sc, player);
            YgoPlayerRunPiles.Trunk(player)?.AddInternal(card, -1, silent: true);
        }

        foreach (SerializableCard sc in pending.Side)
        {
            CardModel card = player.RunState.LoadCard(sc, player);
            YgoPlayerRunPiles.SideDeck(player)?.AddInternal(card, -1, silent: true);
        }

        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
        YgoPlayerMinimumDeck.SetLoadedFromSave(player, pending.LoadedMinimumDeckSize);
        YgoPlayerMinimumDeck.SetReceivedCardProgressLoadedFromSave(player, pending.LoadedMinimumDeckReceivedCardProgress);
        YgoPackRewardProgress.SetOwedRareLoadedFromSave(player, pending.LoadedOwedRareCardVouchers);
        YgoPackRewardProgress.SetPackTagBalanceLoadedFromSave(player, pending.LoadedPackTagBalance);
    }

    public static void Prefix(Player __instance, SerializablePlayer save, ref object? __state)
    {
        __state = null;
        if (__instance.Character is not YgoChar)
            return;

        if (TryStripTrailerFromDeckList(save.Deck, __instance.NetId, "[YgoDuelist][SaveLoad] LoadInventory", out YgoTrunkSideDeckLoadPending? pending))
            __state = pending;
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
