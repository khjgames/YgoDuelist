using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// YGO merchant bundle: grant <see cref="Cards.YgoDuelistCard.BundledCards"/> on purchase; stacked previews on slots.
/// <see cref="MerchantCardEntry.ClearAfterPurchase"/> / <see cref="MerchantCardEntry.RestockAfterPurchase"/> are patched
/// manually from <see cref="MainFile.Initialize"/> — PatchAll often skips multiple method-level targets on one class.
/// </summary>
public static class YgoMerchantShopBundlePurchasePatch
{
    /// <summary>Call after <c>PatchAll</c> so bundle grant prefixes are guaranteed to attach.</summary>
    public static void ApplyMerchantCardEntryPatches(Harmony harmony)
    {
        Type patchType = typeof(YgoMerchantShopBundlePurchasePatch);
        MethodInfo? clear = AccessTools.DeclaredMethod(typeof(MerchantCardEntry), "ClearAfterPurchase");
        if (clear == null)
        {
            MainFile.Logger.Error("[YgoDuelist][ShopBundle] DeclaredMethod MerchantCardEntry.ClearAfterPurchase not found.");
            return;
        }

        harmony.Patch(clear, prefix: new HarmonyMethod(patchType, nameof(PrefixClear)));
        int clearPrefixes = Harmony.GetPatchInfo(clear)?.Prefixes.Count ?? 0;
        MainFile.Logger.Info($"[YgoDuelist][ShopBundle] Patched ClearAfterPurchase (prefix count on method={clearPrefixes}).");

        MethodInfo? restock = AccessTools.DeclaredMethod(
            typeof(MerchantCardEntry),
            "RestockAfterPurchase",
            new[] { typeof(MerchantInventory) });
        if (restock == null)
        {
            MainFile.Logger.Warn("[YgoDuelist][ShopBundle] DeclaredMethod MerchantCardEntry.RestockAfterPurchase not found.");
            return;
        }

        harmony.Patch(restock, prefix: new HarmonyMethod(patchType, nameof(PrefixRestock)));
        int restockPrefixes = Harmony.GetPatchInfo(restock)?.Prefixes.Count ?? 0;
        MainFile.Logger.Info($"[YgoDuelist][ShopBundle] Patched RestockAfterPurchase (prefix count on method={restockPrefixes}).");
    }

    public static void PrefixClear(MerchantCardEntry __instance)
    {
        string id = __instance.CreationResult?.Card?.Id.Entry ?? "(null)";
        YgoMerchantShopBundleDiag.Log($"PrefixClearAfterPurchase card={id}");
        YgoMerchantShopBundlePurchase.ScheduleGrantFromEntry(__instance);
    }

    public static void PrefixRestock(MerchantCardEntry __instance)
    {
        string id = __instance.CreationResult?.Card?.Id.Entry ?? "(null)";
        YgoMerchantShopBundleDiag.Log($"PrefixRestockAfterPurchase card={id}");
        YgoMerchantShopBundlePurchase.ScheduleGrantFromEntry(__instance);
    }
}

/// <summary>Logs when any <see cref="MerchantCardEntry"/> purchase is attempted (click / confirm), before gold/async work.</summary>
[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper), typeof(MerchantInventory), typeof(bool))]
public static class YgoMerchantShopBundleTryPurchaseWrapperDiagPatch
{
    [HarmonyPrefix]
    public static void Prefix(MerchantEntry __instance)
    {
        if (__instance is not MerchantCardEntry mce)
            return;
        string id = mce.CreationResult?.Card?.Id.Entry ?? "(null)";
        YgoMerchantShopBundleDiag.Log($"MerchantCardEntry.OnTryPurchaseWrapper begin card={id} enoughGold={mce.EnoughGold}");
    }
}

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
public static class YgoMerchantShopBundleVisualPatch
{
    private static readonly FieldInfo? HoverTweenField = AccessTools.DeclaredField(typeof(NMerchantSlot), "_hoverTween");
    private static readonly FieldInfo? IsHoveredField = AccessTools.DeclaredField(typeof(NMerchantSlot), "_isHovered");

    private static GridContainer? _deferredResyncYgoBuyGrid;
    private static bool _deferredYgoBuyGridResyncQueued;

    [HarmonyPostfix]
    public static void Postfix(NMerchantCard __instance)
    {
        if (__instance.Entry is not MerchantCardEntry mce)
            return;

        YgoMerchantShopBundleVisual.MountOrRefresh(__instance, mce);
        ResyncYgoBuyGridSlotScale(__instance);
        ScheduleDeferredResyncEntireYgoBuyGrid(__instance);
    }

    /// <summary>
    /// Gold changes and purchase reflow call <see cref="NMerchantCard.UpdateVisual"/> on every slot. Vanilla
    /// <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantSlot"/> may still have an active <c>_hoverTween</c>
    /// tweening <c>scale</c>; assigning scale here without killing it lets the tween override until hover/focus again.
    /// </summary>
    internal static void ResyncYgoBuyGridSlotScale(NMerchantCard slot)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(slot))
            return;

        if (HoverTweenField != null)
        {
            Tween? tw = HoverTweenField.GetValue(slot) as Tween;
            tw?.Kill();
            HoverTweenField.SetValue(slot, null);
        }

        bool hovered = IsHoveredField != null && (bool)IsHoveredField.GetValue(slot)!;
        slot.Scale = Vector2.One * (hovered
            ? YgoMerchantShopLayoutTuning.MerchantSlotHoverScale
            : YgoMerchantShopLayoutTuning.MerchantSlotIdleScale);
    }

    private static void ScheduleDeferredResyncEntireYgoBuyGrid(NMerchantCard slot)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(slot))
            return;
        if (slot.GetParent() is not GridContainer grid)
            return;
        if (_deferredYgoBuyGridResyncQueued && ReferenceEquals(_deferredResyncYgoBuyGrid, grid))
            return;

        _deferredResyncYgoBuyGrid = grid;
        _deferredYgoBuyGridResyncQueued = true;
        Callable.From(ExecuteDeferredResyncEntireYgoBuyGrid).CallDeferred();
    }

    private static void ExecuteDeferredResyncEntireYgoBuyGrid()
    {
        _deferredYgoBuyGridResyncQueued = false;
        GridContainer? grid = _deferredResyncYgoBuyGrid;
        _deferredResyncYgoBuyGrid = null;
        if (!GodotObject.IsInstanceValid(grid))
            return;

        foreach (Node ch in grid.GetChildren())
        {
            if (ch is NMerchantCard nc)
                ResyncYgoBuyGridSlotScale(nc);
        }
    }
}
