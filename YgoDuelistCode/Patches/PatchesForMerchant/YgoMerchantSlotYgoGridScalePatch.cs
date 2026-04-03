using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Vanilla <see cref="NMerchantSlot"/> animates hover on the slot root. YGO buy grid cells are
/// <see cref="GridContainer"/> children — layout forces root <see cref="Control.Scale"/> to (1,1). Hover/idle use
/// <see cref="YgoMerchantShopLayoutTuning.MerchantSlotHoverScale"/> / <see cref="YgoMerchantShopLayoutTuning.MerchantSlotIdleScale"/>
/// on <c>YgoBuyGridScaleRoot</c> (same subtree vanilla scales on the slot root); cell root stays (1,1).
/// </summary>
internal static class YgoAddonBuyGridMerchantSlotIdentifiers
{
    internal static bool IsUnderYgoAddonBuyGrid(NMerchantSlot slot)
    {
        for (Node? n = slot.GetParent(); n != null; n = n.GetParent())
        {
            if (n.Name == "YgoAddonBuyGrid")
                return true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "OnFocus")]
public static class YgoMerchantSlotYgoGridOnFocusPostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantSlot __instance)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(__instance))
            return;
        if (__instance is not NMerchantCard mc)
            return;

        float target = YgoMerchantShopLayoutTuning.MerchantSlotHoverScale;
        YgoMerchantBuyGridScaleTrace.Log(
            $"YgoGridOnFocusPostfix vanilla set slotScale={__instance.Scale} -> chrome hover={target} {YgoMerchantBuyGridScaleTrace.SlotOneLine("ygoFocusPost", __instance)}");

        YgoBuyGridMerchantSlotChromeScale.ApplyChromeHoverInstant(mc);
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "OnUnfocus")]
public static class YgoMerchantSlotYgoGridOnUnfocusPostfix
{
    private static readonly FieldInfo? HoverTweenField = AccessTools.DeclaredField(typeof(NMerchantSlot), "_hoverTween");

    [HarmonyPostfix]
    public static void Postfix(NMerchantSlot __instance)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(__instance))
            return;
        if (__instance is not NMerchantCard mc)
            return;

        YgoMerchantBuyGridScaleTrace.Log(
            $"YgoGridOnUnfocusPostfix START (kill vanilla slot tween; tween YgoBuyGridScaleRoot to idle) {YgoMerchantBuyGridScaleTrace.SlotOneLine("ygoUnfocusPost", __instance)}");

        if (HoverTweenField != null)
        {
            Tween? existing = HoverTweenField.GetValue(__instance) as Tween;
            existing?.Kill();
        }

        __instance.Scale = Vector2.One;

        YgoBuyGridMerchantSlotChromeScale.EnsureBuyGridScaledContentRoot(mc);
        Control? scaleRoot = YgoBuyGridMerchantSlotChromeScale.GetScaleRoot(mc);
        if (scaleRoot == null)
            return;

        Tween tween = __instance.CreateTween();
        HoverTweenField?.SetValue(__instance, tween);

        const float duration = 0.5f;
        YgoBuyGridMerchantSlotChromeScale.TweenChromeToIdle(mc, tween, duration);

        YgoMerchantBuyGridScaleTrace.Log(
            $"YgoGridOnUnfocusPostfix END tween scaleRoot->idle scale={YgoMerchantShopLayoutTuning.MerchantSlotIdleScale} scaleRootNow={scaleRoot.Scale} {YgoMerchantBuyGridScaleTrace.SlotOneLine("ygoUnfocusPost", __instance)}");
    }
}
