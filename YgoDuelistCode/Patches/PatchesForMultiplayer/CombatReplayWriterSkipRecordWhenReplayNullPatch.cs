using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Multiplayer.Replay;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// After MP state divergence the run may abandon while late <see cref="MegaCrit.Sts2.Core.Multiplayer.Messages.Game.ActionEnqueuedMessage"/>
/// packets still arrive. <see cref="CombatReplayWriter"/> then throws because <c>_replay</c> was cleared. Skip recording instead.
/// </summary>
[HarmonyPatch(typeof(CombatReplayWriter), "RecordGameAction")]
public static class CombatReplayWriterSkipRecordWhenReplayNullPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CombatReplayWriter __instance, GameAction gameAction)
    {
        object? replay = Traverse.Create(__instance).Field("_replay").GetValue();
        return replay != null;
    }
}
