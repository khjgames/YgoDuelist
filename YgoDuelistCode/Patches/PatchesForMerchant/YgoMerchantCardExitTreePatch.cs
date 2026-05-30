using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Duplicated YGO buy-grid <see cref="NMerchantCard"/> nodes removed before <see cref="Control._Ready"/>
/// leave <c>_hitbox</c> unset; vanilla <see cref="NMerchantCard._ExitTree"/> then NREs on disconnect.
/// </summary>
[HarmonyPatch(typeof(NMerchantCard), "_ExitTree")]
public static class YgoMerchantCardExitTreePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NMerchantCard __instance)
    {
        if (Traverse.Create(__instance).Field<NClickableControl?>("_hitbox").Value != null)
            return true;

        Traverse.Create(__instance).Field<NCard?>("_cardNode").Value?.QueueFreeSafely();
        return false;
    }
}
