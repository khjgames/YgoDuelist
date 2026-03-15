using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// After targeting, NMouseCardPlay checks IsCardInPlayZone() and cancels if the mouse
/// is outside the zone. Option-row plays skip the drag so the card/mouse may not be in
/// zone; treat option holders as always in play zone so the play proceeds after target selection.
/// </summary>
[HarmonyPatch(typeof(NMouseCardPlay), "IsCardInPlayZone")]
public static class NMouseCardPlayOptionHolderPlayZonePatch
{
    static bool Prefix(NMouseCardPlay __instance, ref bool __result)
    {
        if (__instance.Holder is NYgoOptionCardHolder)
        {
            __result = true;
            return false;
        }
        return true;
    }
}
