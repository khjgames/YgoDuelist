using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// <see cref="NCardPlayQueue.AnimOut"/> and cancellation paths call <c>TweenCardForCancellation</c> for every
/// queued <see cref="NCard"/>. Deferred YGO UI can <see cref="Node.QueueFree"/> a card node while its
/// <c>QueueItem</c> is still present → <see cref="Godot.Tween.TweenProperty"/> throws
/// <see cref="System.ObjectDisposedException"/> (see host/client logs at combat end).
/// </summary>
[HarmonyPatch(typeof(NCardPlayQueue), "TweenCardForCancellation")]
[HarmonyPriority(Priority.First)]
public static class NCardPlayQueueSkipDisposedTweenCancellationPatch
{
    public static bool Prefix(object __0)
    {
        NCard? card = Traverse.Create(__0).Field("card").GetValue() as NCard;
        if (card != null && GodotObject.IsInstanceValid(card))
            return true;

        Tween? tw = Traverse.Create(__0).Field("currentTween").GetValue() as Tween;
        if (tw != null && GodotObject.IsInstanceValid(tw))
            tw.Kill();

        Traverse.Create(__0).Field("currentTween").SetValue(null);
        return false;
    }
}
