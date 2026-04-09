using System;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Runs <see cref="YgoMonsterCommandChecksumReconcile"/> before <see cref="NetFullCombatState.FromRun"/> for the
/// "After player turn start" checksum only — fixes MP desync without blocking Hook.AfterPlayerTurnStart.
/// </summary>
[HarmonyPatch(
    typeof(ChecksumTracker),
    nameof(ChecksumTracker.GenerateChecksum),
    new[] { typeof(string), typeof(GameAction) })]
public static class ChecksumTrackerYgoMonsterCommandReconcilePatch
{
    [HarmonyPrefix]
    public static void Prefix(string context, GameAction? action)
    {
        if (context != YgoMonsterCommandChecksumReconcile.AfterPlayerTurnStartContext)
            return;
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return;

        try
        {
            YgoMonsterCommandChecksumReconcile.ReconcileAfterPlayerTurnStartForChecksum(runState);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist][MP][Checksum][CmdReconcile] failed: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
