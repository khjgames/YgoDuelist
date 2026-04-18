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
/// After HEAL/MEND completes, runs <see cref="CombatStateSynchronizer.StartSync"/> + <see cref="CombatStateSynchronizer.WaitForSync"/>
/// via <see cref="TaskHelper.RunSafely"/> so peers converge on serialized player state.
/// <para/>
/// <b>PreHeal sync was removed.</b> Logs showed the client stuck forever in <c>WaitForSync</c> (no <c>Received sync player message</c>
/// from the host after <c>PreHeal_before</c>) while the host finished — full UI freeze. Running a combat sync barrier <i>inside</i>
/// <see cref="HealRestSiteOption.OnSelect"/> / <see cref="HealRestSiteOption.ExecuteRestSiteHeal"/> interacts badly with the rest-site
/// message flow; PostHeal runs after the option resolves and does not reproduce that hang.
/// </summary>
internal static class YgoRestSiteMpHealSync
{
    /// <summary>Log when a round-trip completes (verbose).</summary>
    public static bool DebugLog;

    /// <summary>Log one line per player before/after PostHeal sync.</summary>
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
