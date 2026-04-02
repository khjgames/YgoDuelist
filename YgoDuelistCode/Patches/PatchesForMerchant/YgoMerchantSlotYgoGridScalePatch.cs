using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Vanilla <see cref="NMerchantSlot"/> hardcodes hover/unhover scale; YGO buy grid slots must follow
/// <see cref="YgoMerchantShopLayoutTuning.MerchantSlotHoverScale"/> and <see cref="YgoMerchantShopLayoutTuning.MerchantSlotIdleScale"/>.
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
        __instance.Scale = Vector2.One * YgoMerchantShopLayoutTuning.MerchantSlotHoverScale;
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "OnUnfocus")]
public static class YgoMerchantSlotYgoGridOnUnfocusPostfix
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantSlot __instance)
    {
        if (!YgoAddonBuyGridMerchantSlotIdentifiers.IsUnderYgoAddonBuyGrid(__instance))
            return;

        var traverse = Traverse.Create(__instance);
        Tween? existing = traverse.Field<Tween?>("_hoverTween").Value;
        existing?.Kill();

        Tween tween = __instance.CreateTween();
        traverse.Field("_hoverTween").SetValue(tween);

        Vector2 idle = Vector2.One * YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
        tween.TweenProperty(__instance, "scale", idle, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }
}
