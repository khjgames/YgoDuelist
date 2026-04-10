using System;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRestSite;

/// <summary>
/// Rest <see cref="HealRestSiteOption"/> runs <see cref="MegaCrit.Sts2.Core.Commands.CreatureCmd.Heal"/> on every peer via
/// <see cref="RestSiteSynchronizer"/>. <see cref="MegaCrit.Sts2.Core.Commands.CreatureCmd.Heal"/> clamps using
/// <c>MaxHp - CurrentHp</c>; if a peer’s <see cref="MegaCrit.Sts2.Core.Entities.Creatures.Creature.CurrentHp"/> for the
/// resting player is wrong, that peer applies a different effective heal than the host. A <see cref="CombatStateSynchronizer"/>
/// round-trip <b>before</b> <see cref="HealRestSiteOption.ExecuteRestSiteHeal"/> aligns HP, then the existing post-option
/// round-trip reapplies serialized players after hooks. Mend uses <see cref="CreatureCmd.Heal"/> directly and already behaved
/// in your session; self-heal is the fragile path.
/// </summary>
internal static class YgoRestSiteMpHealSync
{
    /// <summary>Log when a round-trip completes (verbose).</summary>
    public static bool DebugLog;

    /// <summary>Log one line per player before/after each MP rest sync phase (PreHeal / PostHeal).</summary>
    public static bool LogHpSnapshots = true;

    public static void Register(RestSiteSynchronizer sync)
    {
        sync.AfterPlayerOptionChosen -= OnAfterOption;
        sync.AfterPlayerOptionChosen += OnAfterOption;
    }

    private static void OnAfterOption(RestSiteOption option, bool success, ulong playerId)
    {
        _ = playerId;
        if (!success)
            return;
        if (option.OptionId != "HEAL" && option.OptionId != "MEND")
            return;

        RunManager? rm = RunManager.Instance;
        if (rm == null)
            return;

        Player? owner = Traverse.Create(option).Property<Player>("Owner").Value;
        TaskHelper.RunSafely(RunRoundTripAsync(rm, "PostHeal", owner));
    }

    /// <summary>Runs <see cref="CombatStateSynchronizer.StartSync"/> + <see cref="CombatStateSynchronizer.WaitForSync"/> when MP sync is enabled.</summary>
    internal static async Task RunRoundTripAsync(RunManager rm, string phase, Player? healTargetForLog)
    {
        if (!ShouldRunMpSync(rm, out string? skip))
        {
            if (DebugLog)
                GD.Print($"[YgoDuelist][MP][RestSite][{phase}] skip: {skip}");
            return;
        }

        if (LogHpSnapshots)
            LogAllPlayerHp(phase + "_before", healTargetForLog);

        try
        {
            rm.CombatStateSynchronizer!.StartSync();
            await rm.CombatStateSynchronizer.WaitForSync();
            if (LogHpSnapshots)
                LogAllPlayerHp(phase + "_after", healTargetForLog);
            if (DebugLog)
                GD.Print($"[YgoDuelist][MP][RestSite][{phase}] CombatStateSynchronizer round-trip finished.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist][MP][RestSite][{phase}] player sync failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>Blocking pre-heal sync (Harmony prefix on game thread).</summary>
    internal static void RunRoundTripBlocking(RunManager rm, string phase, Player healTargetForLog)
    {
        if (!ShouldRunMpSync(rm, out string? skip))
        {
            if (DebugLog)
                GD.Print($"[YgoDuelist][MP][RestSite][{phase}] skip: {skip}");
            return;
        }

        try
        {
            RunRoundTripAsync(rm, phase, healTargetForLog).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist][MP][RestSite][{phase}] blocking sync failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static bool ShouldRunMpSync(RunManager rm, out string? skipReason)
    {
        skipReason = null;
        if (rm.CombatStateSynchronizer == null)
        {
            skipReason = "no CombatStateSynchronizer";
            return false;
        }

        if (rm.CombatStateSynchronizer.IsDisabled)
        {
            skipReason = "CombatStateSynchronizer.IsDisabled";
            return false;
        }

        NetGameType net = rm.NetService?.Type ?? NetGameType.None;
        if (net != NetGameType.Host && net != NetGameType.Client)
        {
            skipReason = $"NetGameType={net}";
            return false;
        }

        return true;
    }

    private static void LogAllPlayerHp(string phase, Player? contextPlayer)
    {
        var rs = contextPlayer?.RunState;
        if (rs?.Players == null)
            return;
        foreach (Player p in rs.Players)
        {
            if (p?.Creature == null)
                continue;
            GD.Print(
                $"[YgoDuelist][MP][RestSite][{phase}] netId={p.NetId} isMe={LocalContext.IsMe(p)} hp={p.Creature.CurrentHp}/{p.Creature.MaxHp}");
        }
    }
}

/// <summary>
/// Aligns all peers’ <see cref="MegaCrit.Sts2.Core.Entities.Players.Player"/> / creature HP before
/// <see cref="HealRestSiteOption.ExecuteRestSiteHeal"/> so each machine’s <see cref="MegaCrit.Sts2.Core.Commands.CreatureCmd.Heal"/>
/// clamp matches.
/// </summary>
[HarmonyPatch(typeof(HealRestSiteOption), nameof(HealRestSiteOption.ExecuteRestSiteHeal))]
internal static class YgoHealRestSiteExecutePreSyncPatch
{
    [HarmonyPrefix]
    public static void Prefix(Player player, bool isMimicked)
    {
        _ = isMimicked;
        RunManager? rm = RunManager.Instance;
        if (rm == null || player == null)
            return;
        YgoRestSiteMpHealSync.RunRoundTripBlocking(rm, "PreHeal", player);
    }
}
