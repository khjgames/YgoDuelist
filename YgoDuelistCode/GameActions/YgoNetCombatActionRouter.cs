using System;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Runs;

namespace YgoDuelist.YgoDuelistCode.GameActions;

/// <summary>
/// Single entry point for YgoDuelist combat mutations that must replicate on all multiplayer peers.
/// <para>
/// Slay the Spire 2 MP is <b>lockstep</b>: <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSynchronizer"/>
/// runs the same ordered <see cref="GameAction"/> stream on host and clients. Checksums compare snapshots taken
/// after each action. Anything that changes combat state <i>outside</i> that stream (fire-and-forget tasks, UI-only
/// pile moves, async stance sync before the action completes) can diverge.
/// </para>
/// <para>
/// <b>Already routed through the queue (custom)</b>:
/// <see cref="YgoSetSpellTrapFromHandGameAction"/>, <see cref="YgoMonsterMenuCommandGameAction"/> (option-row menu).
/// Vanilla <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction"/> covers normal hand plays; patches route
/// spell/trap zone and option-pile plays.
/// </para>
/// <para>
/// <b>Still local-only / risky for MP</b> (must eventually become <see cref="GameAction"/> + <c>INetAction</c> if they
/// mutate shared combat state): ad-hoc <see cref="MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely"/> around pile or
/// power work, stance sync queued outside the current <see cref="GameAction"/>, graveyard hooks, etc.
/// Add new net actions sparingly; keep payloads deterministic (model id + ordinals).
/// </para>
/// </summary>
public static class YgoNetCombatActionRouter
{
    /// <summary>True when combat is active and the run uses the MP action synchronizer (host or client).</summary>
    public static bool IsMultiplayerCombatQueueActive =>
        RunManager.Instance?.ActionQueueSynchronizer != null && CombatManager.Instance?.IsInProgress == true;

    /// <summary>
    /// Enqueue <paramref name="action"/> so every peer executes it in order. Logged for MP diagnostics.
    /// Returns false if not in MP combat (caller runs the same logic locally) or enqueue failed.
    /// </summary>
    public static bool TryRequestEnqueue(GameAction action, string sourceTag)
    {
        if (action == null)
            return false;

        if (!IsMultiplayerCombatQueueActive)
            return false;

        try
        {
            RunManager.Instance!.ActionQueueSynchronizer!.RequestEnqueue(action);
            GD.Print($"[YgoDuelist][MP][Queue] {sourceTag} -> {action}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist][MP][Queue] {sourceTag} RequestEnqueue failed: {ex.Message}");
            return false;
        }
    }
}
