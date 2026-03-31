using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Trap set / facedown chrome (portrait overlay, inverted lip border, plaque tint, canvas group mask for portrait window)
/// is applied in <see cref="NCard"/> Reload postfixes (e.g. <c>YgoSetModePlaqueAndFrameTintPatch</c>, <c>YgoSetModePortraitBorderPatch</c>).
/// Hand and most holders call <see cref="NHandCardHolder.UpdateCard"/> which only runs <see cref="NCard.UpdateVisuals"/> — not Reload.
/// Right-click refresh fixes the glitch because <see cref="SpellTrapCardRightClickPatch.RefreshHolder"/> calls both UpdateVisuals and Reload.
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class NCardUpdateVisualsBaseTrapFaceDownSyncPatch
{
    private static readonly MethodInfo? NCardReload = typeof(NCard).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Prefix(NCard __instance)
    {
        if (__instance.Model is not BaseTrapCard trap)
            return;
        trap.NormalizeFaceDownStateForCurrentPile();
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        if (__instance.Model is not BaseTrapCard)
            return;
        if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsNodeReady())
            return;
        NCardReload?.Invoke(__instance, null);
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
public static class NCardReloadBaseTrapFaceDownSyncPatch
{
    public static void Prefix(NCard __instance)
    {
        if (__instance.Model is not BaseTrapCard trap)
            return;
        trap.NormalizeFaceDownStateForCurrentPile();
    }
}
