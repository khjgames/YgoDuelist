using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Runs after other <see cref="NCard.Reload"/> postfixes so mask and border are not overwritten; deferred pass fixes
/// parent scale (compendium / deck grid) applied after the first paint.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class YgoSetFramePortraitCanvasMaskPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyAfter(
        "YgoDuelist.YgoDuelistCode.Patches.YgoSetModePlaqueAndFrameTintPatch",
        "YgoDuelist.YgoDuelistCode.Patches.YgoEquipPortraitOverlayPatch",
        "YgoDuelist.YgoDuelistCode.Patches.YgoCardFrameDrawOrderPatch",
        "YgoDuelist.YgoDuelistCode.Patches.YgoEnergyIconNodePatch")]
    public static void ReloadPostfix(NCard __instance)
    {
        YgoSetFramePortraitCanvasMask.Apply(__instance);
        YgoSetFramePortraitCanvasMask.ApplyDeferred(__instance);
    }
}
