using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shop <see cref="CardModel"/> instances are often mutable copies; <see cref="YgoDuelistCard.BundledCards"/> lives on the template.
/// </summary>
public static class YgoMerchantShopBundleShared
{
    private static readonly FieldInfo? MerchantEntryPlayerField =
        AccessTools.DeclaredField(typeof(MerchantEntry), "_player");
    private static readonly ConditionalWeakTable<MerchantCardEntry, BoxedTagMask> EntryTagMaskTable = new();

    private sealed class BoxedTagMask
    {
        public required YgoCardPackTags Value { get; init; }
    }

    public static Player? GetMerchantEntryPlayer(MerchantEntry entry) =>
        MerchantEntryPlayerField?.GetValue(entry) as Player;

    public static void RegisterEntryTagMask(MerchantCardEntry entry, YgoCardPackTags tagMask)
    {
        EntryTagMaskTable.Remove(entry);
        EntryTagMaskTable.Add(entry, new BoxedTagMask { Value = tagMask });
    }

    public static bool TryGetEntryTagMask(MerchantCardEntry entry, out YgoCardPackTags tagMask)
    {
        if (EntryTagMaskTable.TryGetValue(entry, out BoxedTagMask? boxed))
        {
            tagMask = boxed.Value;
            return true;
        }

        tagMask = YgoCardPackTags.None;
        return false;
    }

    public static bool TryGetBundlingTemplate(CardModel? card, out YgoDuelistCard ygo)
    {
        ygo = null!;
        if (card == null)
            return false;

        bool diag = YgoMerchantShopBundleDiag.IsMaskedBeastDiagCard(card);

        if (card is YgoDuelistCard direct)
        {
            ygo = direct;
            if (diag)
                YgoMerchantShopBundleDiag.Log(
                    $"TryGetBundlingTemplate: OK direct YgoDuelistCard type={direct.GetType().FullName} bundled={direct.BundledCards.Length}");
            return true;
        }

        CardModel canon = card.CanonicalInstance;
        if (canon is YgoDuelistCard canonYgo)
        {
            ygo = canonYgo;
            if (diag)
                YgoMerchantShopBundleDiag.Log(
                    $"TryGetBundlingTemplate: OK via CanonicalInstance cardClr={card.GetType().FullName} canonClr={canon.GetType().FullName} bundled={canonYgo.BundledCards.Length}");
            return true;
        }

        if (diag)
            YgoMerchantShopBundleDiag.Log(
                $"TryGetBundlingTemplate: FAIL cardClr={card.GetType().FullName} canonClr={canon.GetType().FullName} canonIsYgo=False");
        return false;
    }

    /// <summary>
    /// Bundled / bulk-bundled mates for a shop offer (excludes the anchor card). Same resolution as stacked slot previews.
    /// </summary>
    public static void CollectBundledMateCards(
        MerchantCardEntry entry,
        CardModel offerCard,
        YgoDuelistCard bundling,
        List<CardModel> into,
        out ModelId? bulkMateId)
    {
        into.Clear();
        bulkMateId = null;
        ModelId mainId = offerCard.CanonicalInstance.Id;
        foreach (Type bt in bundling.BundledCards)
        {
            CardModel template;
            try
            {
                template = YgoPackCardCatalog.CardFromType(bt);
            }
            catch
            {
                continue;
            }

            if (template.Id == mainId && !bundling.BundleGrantsExtraCopyOfSelf)
                continue;

            into.Add(template);
        }

        Player? shopPlayer = GetMerchantEntryPlayer(entry);
        YgoCardPackTags rowTagMask = TryGetEntryTagMask(entry, out YgoCardPackTags mask)
            ? mask
            : YgoCardPackTags.None;
        if (bundling.BulkBundled && shopPlayer != null
            && YgoBulkBundledResolver.TryGetMerchantBulkMateTemplate(entry, shopPlayer, bundling, rowTagMask, out CardModel? bulkMate)
            && bulkMate != null)
        {
            into.Add(bulkMate);
            bulkMateId = bulkMate.Id;
        }
    }

    /// <summary>
    /// Full inspect-carousel list for a YGO bundle shop slot: anchor offer first, then bundled / bulk mates.
    /// </summary>
    public static bool TryBuildShopInspectCarousel(
        MerchantCardEntry entry,
        CardModel offerCard,
        out List<CardModel> carousel,
        out int startIndex)
    {
        carousel = new List<CardModel>();
        startIndex = 0;

        if (!TryGetBundlingTemplate(offerCard, out YgoDuelistCard bundling))
            return false;

        bool hasExplicitBundle = bundling.BundledCards.Length > 0;
        bool hasBulk = bundling.BulkBundled && GetMerchantEntryPlayer(entry) != null;
        if (!hasExplicitBundle && !hasBulk)
            return false;

        var mates = new List<CardModel>();
        CollectBundledMateCards(entry, offerCard, bundling, mates, out _);
        if (mates.Count == 0)
            return false;

        carousel.Add(offerCard);
        carousel.AddRange(mates);
        return true;
    }
}
