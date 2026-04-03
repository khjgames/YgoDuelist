using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Harmony-only probes for YGO buy grid scale (grep godot.log for <see cref="YgoMerchantBuyGridScaleTrace.Tag"/>).
/// </summary>
internal static class YgoMerchantBuyGridScaleTracePatches
{
    public const int TraceEarly = 800;
    public const int TraceLate = -800;
}

[HarmonyPatch(typeof(NMerchantSlot), "OnFocus")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceEarly)]
internal static class YgoMerchantBuyGridTraceNMerchantSlotOnFocusPrefix
{
    [HarmonyPrefix]
    public static void Prefix(NMerchantSlot __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantSlot.OnFocus PREFIX (before vanilla)", __instance));
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "OnFocus")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceLate)]
internal static class YgoMerchantBuyGridTraceNMerchantSlotOnFocusPostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantSlot __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantSlot.OnFocus POSTFIX (after all postfixes)", __instance));
        YgoMerchantBuyGridScaleTrace.DumpEntireGridFromSlot("OnFocus", __instance);
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "OnUnfocus")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceEarly)]
internal static class YgoMerchantBuyGridTraceNMerchantSlotOnUnfocusPrefix
{
    [HarmonyPrefix]
    public static void Prefix(NMerchantSlot __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantSlot.OnUnfocus PREFIX (before vanilla)", __instance));
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "OnUnfocus")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceLate)]
internal static class YgoMerchantBuyGridTraceNMerchantSlotOnUnfocusPostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantSlot __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantSlot.OnUnfocus POSTFIX (after YGO tween postfixes)", __instance));
        YgoMerchantBuyGridScaleTrace.DumpEntireGridFromSlot("OnUnfocus", __instance);
    }
}

/// <summary>Runs when <see cref="NMerchantCard.UpdateVisual"/> calls <c>base.UpdateVisual()</c>.</summary>
[HarmonyPatch(typeof(NMerchantSlot), "UpdateVisual")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceEarly)]
internal static class YgoMerchantBuyGridTraceNMerchantSlotUpdateVisualPrefix
{
    [HarmonyPrefix]
    public static void Prefix(NMerchantSlot __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantSlot.UpdateVisual PREFIX (base entry)", __instance));
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "UpdateVisual")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceLate)]
internal static class YgoMerchantBuyGridTraceNMerchantSlotUpdateVisualPostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantSlot __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantSlot.UpdateVisual POSTFIX (base exit)", __instance));
    }
}

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceEarly)]
internal static class YgoMerchantBuyGridTraceNMerchantCardUpdateVisualPrefix
{
    [HarmonyPrefix]
    public static void Prefix(NMerchantCard __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantCard.UpdateVisual PREFIX (override entry)", __instance));
    }
}

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceLate)]
internal static class YgoMerchantBuyGridTraceNMerchantCardUpdateVisualPostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantCard __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantCard.UpdateVisual POSTFIX (after Resync postfix)", __instance));
    }
}

[HarmonyPatch(typeof(NMerchantCard), "OnSuccessfulPurchase", typeof(PurchaseStatus), typeof(MerchantEntry))]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceEarly)]
internal static class YgoMerchantBuyGridTraceNMerchantCardOnSuccessfulPurchasePrefix
{
    [HarmonyPrefix]
    public static void Prefix(NMerchantCard __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantCard.OnSuccessfulPurchase PREFIX", __instance));
        YgoMerchantBuyGridScaleTrace.DumpEntireGridFromSlot("OnSuccessfulPurchase PREFIX", __instance);
    }
}

[HarmonyPatch(typeof(NMerchantCard), "OnSuccessfulPurchase", typeof(PurchaseStatus), typeof(MerchantEntry))]
[HarmonyPriority(YgoMerchantBuyGridScaleTracePatches.TraceLate)]
internal static class YgoMerchantBuyGridTraceNMerchantCardOnSuccessfulPurchasePostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantCard __instance)
    {
        if (!YgoMerchantBuyGridScaleTrace.IsYgoBuyGridSlot(__instance))
            return;
        YgoMerchantBuyGridScaleTrace.Log(YgoMerchantBuyGridScaleTrace.SlotOneLine("NMerchantCard.OnSuccessfulPurchase POSTFIX", __instance));
        YgoMerchantBuyGridScaleTrace.DumpEntireGridFromSlot("OnSuccessfulPurchase POSTFIX", __instance);
    }
}
