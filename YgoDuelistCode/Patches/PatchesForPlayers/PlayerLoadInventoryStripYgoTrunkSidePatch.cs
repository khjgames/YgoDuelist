using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
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
            return;

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
    }

    public static void Postfix(Player __instance, object? __state)
    {
        if (__state is not YgoTrunkSideDeckLoadPending pending)
            return;

        foreach (SerializableCard sc in pending.Extra)
        {
            CardModel card = __instance.RunState.LoadCard(sc, __instance);
            PlayerRunExtraDeck.GetOrCreatePile(__instance).AddInternal(card, -1, silent: true);
        }

        foreach (SerializableCard sc in pending.Trunk)
        {
            CardModel card = __instance.RunState.LoadCard(sc, __instance);
            PlayerRunTrunk.GetOrCreatePile(__instance).AddInternal(card, -1, silent: true);
        }

        foreach (SerializableCard sc in pending.Side)
        {
            CardModel card = __instance.RunState.LoadCard(sc, __instance);
            PlayerRunSideDeck.GetOrCreatePile(__instance).AddInternal(card, -1, silent: true);
        }

        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(__instance);
        YgoPlayerMinimumDeck.SetLoadedFromSave(__instance, pending.LoadedMinimumDeckSize);
        YgoPackRewardProgress.SetOwedRareLoadedFromSave(__instance, pending.LoadedOwedRareCardVouchers);
    }
}
