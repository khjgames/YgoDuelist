using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Runs before <see cref="NCardPlayQueue.OnActionEnqueued"/> builds the play-queue <c>NCard</c> from
/// <see cref="PlayCardAction.NetCombatCard"/> so spell/trap activations use the zone instance, not a same-id hand copy.
/// </summary>
[HarmonyPatch(typeof(NCardPlayQueue), "OnActionEnqueued")]
[HarmonyPriority(Priority.First)]
public static class NCardPlayQueueOnActionEnqueuedSpellTrapNetRebindPatch
{
    private static void Prefix(GameAction action)
    {
        if (action is PlayCardAction pca)
            YgoSpellTrapPlayCardMpResolver.TryRebindNetCombatCardIfSpellTrapZone(pca);
    }
}
