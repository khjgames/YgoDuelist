using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// MP: Stiff/Fatigue on duel pets must match on every peer when <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.ChecksumTracker"/>
/// snapshots state. A Harmony postfix on <see cref="MegaCrit.Sts2.Core.Hooks.Hook.AfterPlayerTurnStart"/> that uses
/// fire-and-forget async work can still be in flight when the game immediately hashes "After player turn start". Do not block
/// with <c>PowerCmd.Remove</c> + <c>GetResult()</c> — that awaits timed waits and can freeze the host. Use
/// <see cref="MonsterCommandRegistry.ResetCommandLockStateSyncForChecksum"/> instead.
/// </summary>
public static class YgoMonsterCommandChecksumReconcile
{
    public const string AfterPlayerTurnStartContext = "After player turn start";

    /// <summary>
    /// Clears monster-command lock state for all pets that participate in <see cref="MonsterCommandRegistry"/> so
    /// Stiff/Fatigue powers match across peers at the "After player turn start" checkpoint.
    /// </summary>
    public static void ReconcileAfterPlayerTurnStartForChecksum(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.PlayerCombatState == null)
                continue;

            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (!MonsterCommandRegistry.TryGet(pet, out _))
                    continue;

                bool had = pet.HasPower<StiffPower>() || pet.HasPower<FatiguePower>();
                MonsterCommandRegistry.ResetCommandLockStateSyncForChecksum(pet);
                if (had)
                {
                    GD.Print(
                        $"[YgoDuelist][MP][Checksum][CmdReconcile] cleared stiff/fatigue before snapshot petCombatId={pet.CombatId} netId={player.NetId}");
                }
            }
        }
    }
}
