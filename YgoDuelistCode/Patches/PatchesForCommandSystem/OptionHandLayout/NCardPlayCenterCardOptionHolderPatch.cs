using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// CenterCard() only sets the holder's target position (tweened). For option holders we skip
/// the drag, so the holder is still at Y=-745 when targeting starts and the red arrow draws
/// from off-screen. Force the option holder to the center position immediately so the arrow
/// is visible and the user can click an enemy.
/// </summary>
[HarmonyPatch(typeof(NCardPlay), "CenterCard")]
public static class NCardPlayCenterCardOptionHolderPatch
{
    static void Postfix(NCardPlay __instance)
    {
        if (__instance.Holder is not NYgoOptionCardHolder holder)
            return;

        holder.Position = holder.TargetPosition;
        // Anchor for drag limit in parent-local space so SetTargetPosition clamp works regardless of viewport transform.
        holder.OptionDragAnchor = holder.Position;
    }
}
