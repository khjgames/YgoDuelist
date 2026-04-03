using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// YGO merchant bundle: grant <see cref="Cards.YgoDuelistCard.BundledCards"/> on purchase; stacked previews on slots.
/// </summary>
public static class YgoMerchantShopBundlePurchasePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MerchantCardEntry), "ClearAfterPurchase")]
    public static void PrefixClear(MerchantCardEntry __instance)
    {
        string id = __instance.CreationResult?.Card?.Id.Entry ?? "(null)";
        YgoMerchantShopBundleDiag.Log($"Harmony PrefixClearAfterPurchase card={id}");
        YgoMerchantShopBundlePurchase.ScheduleGrantFromEntry(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MerchantCardEntry), "RestockAfterPurchase")]
    public static void PrefixRestock(MerchantCardEntry __instance)
    {
        string id = __instance.CreationResult?.Card?.Id.Entry ?? "(null)";
        YgoMerchantShopBundleDiag.Log($"Harmony PrefixRestockAfterPurchase card={id}");
        YgoMerchantShopBundlePurchase.ScheduleGrantFromEntry(__instance);
    }
}

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
public static class YgoMerchantShopBundleVisualPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantCard __instance)
    {
        if (__instance.Entry is not MerchantCardEntry mce)
            return;

        YgoMerchantShopBundleVisual.MountOrRefresh(__instance, mce);
        ResyncYgoBuyGridSlotScale(__instance);
    }

    /// <summary>
    /// Gold changes and other updates call <see cref="NMerchantCard.UpdateVisual"/> without re-running focus tweens;
    /// slot <see cref="Control.Scale"/> can snap to 1. Re-apply YGO idle/hover scale for addon-grid cells.
    /// </summary>
    private static void ResyncYgoBuyGridSlotScale(NMerchantCard slot)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(slot))
            return;

        bool hovered = Traverse.Create(slot).Field<bool>("_isHovered").Value;
        slot.Scale = Vector2.One * (hovered
            ? YgoMerchantShopLayoutTuning.MerchantSlotHoverScale
            : YgoMerchantShopLayoutTuning.MerchantSlotIdleScale);
    }
}
