using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Deferred UI (e.g. spell/trap <see cref="Services.YgoSpellTrapZoneAfterPlayUi.ScheduleCleanup"/>) can <see cref="Godot.Node.QueueFree"/>
/// an <see cref="NCard"/> that is still listed in <see cref="NCardPlayQueue"/>. The next
/// <see cref="NCardPlayQueue.TweenAllToQueuePosition"/> then touches a disposed node →
/// <see cref="System.ObjectDisposedException"/> and MP checksum divergence on unrelated plays (e.g. hand Strikes).
/// </summary>
[HarmonyPatch(typeof(NCardPlayQueue), "TweenAllToQueuePosition")]
[HarmonyPriority(Priority.First)]
public static class NCardPlayQueuePruneDisposedBeforeTweenPatch
{
    private static void Prefix(NCardPlayQueue __instance)
    {
        IList? list = Traverse.Create(__instance).Field("_playQueue").GetValue<IList>();
        if (list == null || list.Count == 0)
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            object? item = list[i];
            if (item == null)
            {
                list.RemoveAt(i);
                continue;
            }

            NCard? card = Traverse.Create(item).Field("card").GetValue() as NCard;
            if (card == null || !GodotObject.IsInstanceValid(card))
                list.RemoveAt(i);
        }
    }
}
