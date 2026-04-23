using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
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

        CardPile? trunk = YgoPlayerRunPiles.Trunk(__instance);
        CardPile? side = YgoPlayerRunPiles.SideDeck(__instance);
        if (trunk == null || side == null)
            return;
        int tc = trunk.Cards.Count;
        int sc = side.Cards.Count;
        int ec = YgoPlayerRunPiles.RunExtraDeckIfExists(__instance)?.Cards.Count ?? 0;
        ec = Math.Clamp(ec, 0, YgoSaveTrunkSideMarkerCard.MaxSerializedPileCount);
        int minDeck = YgoPlayerMinimumDeck.Get(__instance);
        YgoPackRewardProgressState packProgress = YgoPackRewardProgress.For(__instance);
        int owedRare = packProgress.OwedRareCardVouchers;
        bool needTagBalance = packProgress.HasPackTagBalanceToPersist();
        bool needTrailer = tc > 0 || sc > 0 || ec > 0
            || minDeck > YgoPlayerMinimumDeck.StartingMinimum
            || owedRare > 0
            || needTagBalance;
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
        if (owedRare > 0)
        {
            intProps.Add(
                new SavedProperties.SavedProperty<int>(
                    YgoSaveTrunkSideMarkerCard.OwedRareCardVouchersProp,
                    Math.Clamp(owedRare, 0, YgoSaveTrunkSideMarkerCard.MaxSerializedOwedRareVouchers)));
        }

        var stringProps = new List<SavedProperties.SavedProperty<string>>();
        if (needTagBalance)
        {
            string blob = YgoPackTagBalanceSerializer.Serialize(packProgress.PackTagAccumByFlag);
            stringProps.Add(
                new SavedProperties.SavedProperty<string>(YgoSaveTrunkSideMarkerCard.PackTagBalanceProp, blob));
        }

        if (intProps.Count > 0 || stringProps.Count > 0)
        {
            marker.Props = new SavedProperties();
            if (intProps.Count > 0)
                marker.Props.ints = intProps;
            if (stringProps.Count > 0)
                marker.Props.strings = stringProps;
        }

        deck.Add(marker);
        GD.Print(
            $"[YgoDuelist][SaveLoad] ToSerializable trailer appended netId={__instance.NetId} " +
            $"extra={ec} trunk={tc} side={sc} minDeck={minDeck} owedRare={owedRare} packTagBalance={needTagBalance} deckCount={deck.Count}");
    }
}
