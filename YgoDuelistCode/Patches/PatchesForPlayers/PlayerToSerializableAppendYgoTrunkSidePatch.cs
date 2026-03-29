using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoChar = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Appends trunk, side, then marker after main deck and extra-deck cards. Marker stores extra count in props when needed.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.ToSerializable))]
[HarmonyAfter("YgoDuelist.YgoDuelistCode.Patches.PlayerToSerializableAppendYgoExtraDeckPatch")]
public static class PlayerToSerializableAppendYgoTrunkSidePatch
{
    public static void Postfix(Player __instance, ref SerializablePlayer __result)
    {
        if (__instance.Character is not YgoChar)
            return;

        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(__instance);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(__instance);
        int tc = trunk.Cards.Count;
        int sc = side.Cards.Count;
        int ec = PlayerRunExtraDeck.GetPileIfExists(__instance)?.Cards.Count ?? 0;
        ec = Math.Clamp(ec, 0, YgoSaveTrunkSideMarkerCard.MaxSerializedPileCount);
        int minDeck = YgoPlayerMinimumDeck.Get(__instance);
        bool needTrailer = tc > 0 || sc > 0 || ec > 0 || minDeck > YgoPlayerMinimumDeck.StartingMinimum;
        if (!needTrailer)
            return;

        List<SerializableCard> deck = __result.Deck;
        foreach (CardModel c in trunk.Cards)
            deck.Add(c.ToSerializable());
        foreach (CardModel c in side.Cards)
            deck.Add(c.ToSerializable());

        ModelId markerId = ModelDb.Card<YgoSaveTrunkSideMarkerCard>().Id;
        var marker = new SerializableCard
        {
            Id = markerId,
            CurrentUpgradeLevel = Math.Clamp(tc, 0, YgoSaveTrunkSideMarkerCard.MaxSerializedPileCount),
            FloorAddedToDeck = Math.Clamp(sc, 0, YgoSaveTrunkSideMarkerCard.MaxSerializedPileCount)
        };
        var intProps = new List<SavedProperties.SavedProperty<int>>();
        if (ec > 0)
            intProps.Add(new SavedProperties.SavedProperty<int>(YgoSaveTrunkSideMarkerCard.ExtraDeckCountProp, ec));
        if (minDeck > YgoPlayerMinimumDeck.StartingMinimum)
            intProps.Add(new SavedProperties.SavedProperty<int>(YgoSaveTrunkSideMarkerCard.MinDeckSizeProp, minDeck));

        if (intProps.Count > 0)
        {
            marker.Props = new SavedProperties { ints = intProps };
        }

        deck.Add(marker);
    }
}
