using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// After vanilla logs MP checksum mismatch, emit compact YGO pile lines (same fingerprint as <see cref="NetFullCombatStateYgoChecksumPatch"/>)
/// so host vs client logs stay comparable without diffing full <see cref="NetFullCombatState"/> dumps.
/// </summary>
internal static class ChecksumTrackerDivergenceYgoLogPatch
{
    [HarmonyPatch(typeof(ChecksumTracker), "CompareChecksums")]
    [HarmonyPostfix]
    public static void CompareChecksumsPostfix(ChecksumTracker __instance, object[] __args)
    {
        if (__args.Length < 3)
            return;
        object? localChecksum = __args[0];
        if (localChecksum == null || __args[1] is not NetChecksumData remoteChecksum || __args[2] is not ulong remoteId)
            return;

        object? dataObj = AccessTools.Field(localChecksum.GetType(), "data")?.GetValue(localChecksum);
        if (dataObj is not NetChecksumData data)
            return;
        if (data.checksum == remoteChecksum.checksum)
            return;

        var fullState = AccessTools.Field(localChecksum.GetType(), "fullState")?.GetValue(localChecksum) as NetFullCombatState;
        var context = AccessTools.Field(localChecksum.GetType(), "context")?.GetValue(localChecksum) as string;
        var runState = AccessTools.Field(typeof(ChecksumTracker), "_runState")?.GetValue(__instance) as IRunState;

        NetFullCombatStateYgoChecksumPatch.LogYgoFingerprintsOnHostCompareMismatch(
            runState,
            fullState,
            remoteId,
            data.id,
            data.checksum,
            remoteChecksum.checksum,
            context);
    }

    [HarmonyPatch(typeof(ChecksumTracker), "LogStateDivergence")]
    [HarmonyPostfix]
    public static void LogStateDivergencePostfix(ChecksumTracker __instance, object[] __args)
    {
        if (__args.Length < 3)
            return;
        object? localChecksum = __args[0];
        if (localChecksum == null || __args[1] is not StateDivergenceMessage message || __args[2] is not ulong remoteId)
            return;

        object? dataObj = AccessTools.Field(localChecksum.GetType(), "data")?.GetValue(localChecksum);
        if (dataObj is not NetChecksumData data)
            return;

        var localFull = AccessTools.Field(localChecksum.GetType(), "fullState")?.GetValue(localChecksum) as NetFullCombatState;
        var context = AccessTools.Field(localChecksum.GetType(), "context")?.GetValue(localChecksum) as string;
        var runState = AccessTools.Field(typeof(ChecksumTracker), "_runState")?.GetValue(__instance) as IRunState;

        NetFullCombatStateYgoChecksumPatch.LogYgoFingerprintsOnClientDivergenceMessage(
            runState,
            localFull,
            message.senderCombatState,
            remoteId,
            data.id,
            data.checksum,
            message.senderChecksum.checksum,
            context);
    }
}
