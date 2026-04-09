using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Replay;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// MP teardown: <see cref="MegaCrit.Sts2.Core.Runs.RunManager.StateDiverged"/> calls
/// <see cref="CombatReplayWriter.WriteReplay"/> with <c>stopRecording: true</c>, which clears <c>_replay</c>.
/// The <see cref="MegaCrit.Sts2.Core.GameActions.ActionExecutor"/> queue can still run host-originated
/// <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction"/>s afterward; post-action checksum recording invokes
/// private <c>RecordChecksum</c>, which throws when <c>_replay</c> is null and combat is still considered in progress
/// (<c>RecordInitialState must be called first</c>). Skip recording in that window so teardown does not fault.
/// </summary>
[HarmonyPatch(typeof(CombatReplayWriter), "RecordChecksum")]
public static class CombatReplayWriterRecordChecksumMpPatch
{
    static bool Prefix(CombatReplayWriter __instance)
    {
        object? replay = Traverse.Create(__instance).Field<object>("_replay").Value;
        if (replay != null)
            return true;

        if (!__instance.IsEnabled || CombatManager.Instance?.IsInProgress != true)
            return true;

        GD.Print(
            "[YgoDuelist][MP] CombatReplayWriter.RecordChecksum: skipped (replay cleared while combat still draining; typical after state divergence)");
        return false;
    }
}
