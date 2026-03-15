using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When an option holder is returned to hand after a cancelled drag, the stored index
/// can be out of range (e.g. 7 when the container only has 5 children after we removed
/// other option holders). Clamp the index so MoveChild does not throw.
/// </summary>
[HarmonyPatch(typeof(NPlayerHand))]
public static class NPlayerHandReturnHolderToHandPatch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(NPlayerHand), "ReturnHolderToHand");
    }

    static bool Prefix(NPlayerHand __instance, NHandCardHolder holder)
    {
        if (holder is not NYgoOptionCardHolder)
            return true;

        var queueField = AccessTools.Field(typeof(NPlayerHand), "_holdersAwaitingQueue");
        var queue = queueField?.GetValue(__instance) as Dictionary<NHandCardHolder, int>;

        if (!GodotObject.IsInstanceValid(holder))
        {
            if (queue != null)
            {
                try { queue.Remove(holder); }
                catch (System.ObjectDisposedException) { }
            }
            if (YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand == holder)
                YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand = null;
            return false;
        }

        if (!__instance.IsAwaitingPlay(holder))
            return true;

        if (queue == null || !queue.TryGetValue(holder, out int index))
            return true;

        queue.Remove(holder);
        var container = __instance.CardHolderContainer;
        int count = container.GetChildCount();
        int clampedIndex = Mathf.Clamp(index, 0, count);

        holder.Reparent(container);
        if (clampedIndex >= 0)
            container.MoveChild(holder, clampedIndex);
        holder.SetDefaultTargets();
        holder.Visible = true;

        if (YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand == holder)
        {
            YgoOptionHandUiPatch.PendingOptionHolderToFreeAfterReturnToHand = null;
            container.RemoveChild(holder);
            holder.QueueFree();
        }
        else
        {
            // Force option row to rebuild from pile so the cancelled card is guaranteed to show.
            var player = holder.CardModel?.Owner as Player;
            if (player != null)
            {
                var tree = __instance.GetTree();
                var timer = tree.CreateTimer(0.0);
                timer.Timeout += () => YgoOptionHandBridge.SyncFromOptionPile(player);
            }
        }

        return false;
    }
}
