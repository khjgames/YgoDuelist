using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Skips <see cref="NCardPlayQueue.OnActionEnqueued"/> when the play uses a custom
/// <see cref="PlayCardAction.ExecuteAction"/> path that only calls
/// <see cref="NCardPlayQueue.UpdateCardBeforeExecution"/> after cancelable grids / async prep (see
/// <see cref="YgoPlayCardQueueDeferral"/>).
/// Runs after <see cref="NCardPlayQueueOnActionEnqueuedSpellTrapNetRebindPatch"/> so <see cref="PlayCardAction.NetCombatCard"/> is correct.
/// </summary>
[HarmonyPatch(typeof(NCardPlayQueue), "OnActionEnqueued")]
[HarmonyPriority(400)]
public static class NCardPlayQueueDeferEnqueueForPreConfirmPlayPatch
{
    private static bool Prefix(GameAction action)
    {
        if (action is not PlayCardAction pca || !YgoPlayCardQueueDeferral.ShouldSkipOnActionEnqueued(pca, out string? reason))
            return true;

        GD.Print(
            $"[YgoDuelist][Queue][Defer] Skipping OnActionEnqueued until execute after confirm ({reason}, player {pca.Player.NetId}, card {pca.CardModelId.Entry})");
        return false;
    }
}
