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
        YgoMerchantBuyGridScaleTrace.Log($"PrefixClearAfterPurchase (entry pipeline) card={id}");
        YgoMerchantShopBundlePurchase.ScheduleGrantFromEntry(__instance);
    }

    public static void PrefixRestock(MerchantCardEntry __instance)
    {
        string id = __instance.CreationResult?.Card?.Id.Entry ?? "(null)";
        YgoMerchantShopBundleDiag.Log($"PrefixRestockAfterPurchase card={id}");
        YgoMerchantBuyGridScaleTrace.Log($"PrefixRestockAfterPurchase (entry pipeline) card={id}");
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
        YgoMerchantBuyGridScaleTrace.Log($"MerchantCardEntry.OnTryPurchaseWrapper begin (scale trace) card={id} enoughGold={mce.EnoughGold}");
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

        if (YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(__instance))
        {
            YgoMerchantBuyGridScaleTrace.Log(
                $"YgoMerchantShopBundleVisualPatch.Postfix BEFORE MountOrRefresh {YgoMerchantBuyGridScaleTrace.SlotOneLine("bundlePost", __instance)}");
        }

        YgoMerchantShopBundleVisual.MountOrRefresh(__instance, mce);
        ResyncYgoBuyGridSlotScale(__instance);
        ScheduleDeferredResyncEntireYgoBuyGrid(__instance);

        if (YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(__instance))
        {
            YgoMerchantBuyGridScaleTrace.Log(
                $"YgoMerchantShopBundleVisualPatch.Postfix AFTER defer schedule {YgoMerchantBuyGridScaleTrace.SlotOneLine("bundlePost", __instance)}");
        }
    }

    /// <summary>
    /// YGO buy grid: <see cref="GridContainer"/> resets each cell <see cref="NMerchantCard"/>'s <see cref="Control.Scale"/>
    /// to (1,1) during layout. Idle/hover sizing is applied on <c>YgoBuyGridScaleRoot</c>; the slot root stays (1,1).
    /// </summary>
    internal static void ResyncYgoBuyGridSlotScale(NMerchantCard slot)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(slot))
            return;

        Control? scaleRootBefore = YgoBuyGridMerchantSlotChromeScale.GetScaleRoot(slot);
        Vector2 scaleBeforeSlot = slot.Scale;
        Vector2 scaleBeforeRoot = scaleRootBefore?.Scale ?? new Vector2(-1f, -1f);
        bool hadTween = HoverTweenField != null && HoverTweenField.GetValue(slot) is Tween;
        bool hoveredBefore = IsHoveredField != null && (bool)IsHoveredField.GetValue(slot)!;
        YgoMerchantBuyGridScaleTrace.Log(
            $"ResyncYgoBuyGridSlotScale ENTER hadTween={hadTween} hovered={hoveredBefore} slotBefore={scaleBeforeSlot} scaleRootBefore={scaleBeforeRoot} {YgoMerchantBuyGridScaleTrace.SlotOneLine("resyncIn", slot)}");

        if (HoverTweenField != null)
        {
            Tween? tw = HoverTweenField.GetValue(slot) as Tween;
            tw?.Kill();
            HoverTweenField.SetValue(slot, null);
        }

        bool hovered = IsHoveredField != null && (bool)IsHoveredField.GetValue(slot)!;
        float target = hovered
            ? YgoMerchantShopLayoutTuning.MerchantSlotHoverScale
            : YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
        YgoBuyGridMerchantSlotChromeScale.ApplyChromeUniformScale(slot, target);

        Control? scaleRootAfter = YgoBuyGridMerchantSlotChromeScale.GetScaleRoot(slot);
        YgoMerchantBuyGridScaleTrace.Log(
            $"ResyncYgoBuyGridSlotScale EXIT target={target} slotAfter={slot.Scale} scaleRootAfter={(scaleRootAfter == null ? "null" : scaleRootAfter.Scale.ToString())} {YgoMerchantBuyGridScaleTrace.SlotOneLine("resyncOut", slot)}");
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
        YgoMerchantBuyGridScaleTrace.Log($"ScheduleDeferredResyncEntireYgoBuyGrid QUEUED grid={grid.GetPath()} {YgoMerchantBuyGridScaleTrace.GridSummary(grid)}");
        Callable.From(ExecuteDeferredResyncEntireYgoBuyGrid).CallDeferred();
    }

    private static void ExecuteDeferredResyncEntireYgoBuyGrid()
    {
        _deferredYgoBuyGridResyncQueued = false;
        GridContainer? grid = _deferredResyncYgoBuyGrid;
        _deferredResyncYgoBuyGrid = null;
        if (!GodotObject.IsInstanceValid(grid))
        {
            YgoMerchantBuyGridScaleTrace.Log("ExecuteDeferredResyncEntireYgoBuyGrid ABORT grid invalid");
            return;
        }

        YgoMerchantBuyGridScaleTrace.Log("ExecuteDeferredResyncEntireYgoBuyGrid BEGIN (before per-slot Resync)");
        YgoMerchantBuyGridScaleTrace.DumpEntireGrid("ExecuteDeferred BEFORE", grid);

        foreach (Node ch in grid.GetChildren())
        {
            if (ch is NMerchantCard nc)
                ResyncYgoBuyGridSlotScale(nc);
        }

        YgoMerchantBuyGridScaleTrace.Log("ExecuteDeferredResyncEntireYgoBuyGrid END (after per-slot Resync)");
        YgoMerchantBuyGridScaleTrace.DumpEntireGrid("ExecuteDeferred AFTER", grid);
    }
}
