using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Right-click shop preview on bundle / bulk-bundle offers opens <see cref="NInspectCardScreen"/> with left/right arrows
/// cycling the anchor card and every card included in the purchase (same set as stacked slot previews).
/// </summary>
[HarmonyPatch(typeof(NMerchantCard), "OnPreview")]
public static class YgoMerchantShopBundleInspectPatch
{
    private static readonly AccessTools.FieldRef<NMerchantCard, MerchantCardEntry> CardEntryField =
        AccessTools.FieldRefAccess<NMerchantCard, MerchantCardEntry>("_cardEntry");

    private static readonly AccessTools.FieldRef<NMerchantCard, NCard?> CardNodeField =
        AccessTools.FieldRefAccess<NMerchantCard, NCard?>("_cardNode");

    private static readonly MethodInfo? ClearHoverTipMethod =
        AccessTools.DeclaredMethod(typeof(NMerchantSlot), "ClearHoverTip");

    [HarmonyPrefix]
    public static bool Prefix(NMerchantCard __instance)
    {
        MerchantCardEntry entry = CardEntryField(__instance);
        CardModel? offer = CardNodeField(__instance)?.Model ?? entry.CreationResult?.Card;
        if (offer == null)
            return true;

        if (!YgoMerchantShopBundleShared.TryBuildShopInspectCarousel(entry, offer, out List<CardModel> carousel, out int startIndex))
            return true;

        ClearHoverTipMethod?.Invoke(__instance, null);
        NGame.Instance?.GetInspectCardScreen().Open(carousel, startIndex);
        return false;
    }
}
