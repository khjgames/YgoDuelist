using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Tracks hovered/selected spell-trap-zone equip in hand so duel monster portraits can show the equip link overlay.
/// </summary>
[HarmonyPatch(typeof(HoveredModelTracker))]
public static class YgoHoveredZoneEquipHandPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HoveredModelTracker.OnLocalCardHovered))]
    private static void AfterCardHovered(HoveredModelTracker __instance, CardModel cardModel) =>
        YgoZoneEquipHandHoverState.RecomputeFromTracker(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HoveredModelTracker.OnLocalCardUnhovered))]
    private static void AfterCardUnhovered(HoveredModelTracker __instance) =>
        YgoZoneEquipHandHoverState.RecomputeFromTracker(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HoveredModelTracker.OnLocalCardSelected))]
    private static void AfterCardSelected(HoveredModelTracker __instance, CardModel cardModel) =>
        YgoZoneEquipHandHoverState.RecomputeFromTracker(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HoveredModelTracker.OnLocalCardDeselected))]
    private static void AfterCardDeselected(HoveredModelTracker __instance) =>
        YgoZoneEquipHandHoverState.RecomputeFromTracker(__instance);
}
