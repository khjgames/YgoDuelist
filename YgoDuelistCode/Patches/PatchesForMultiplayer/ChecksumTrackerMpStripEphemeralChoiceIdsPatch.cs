using System.Collections.Generic;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// <see cref="PlayerChoiceSynchronizer"/> advances <see cref="NetFullCombatState.nextChoiceIds"/> per slot when
/// <c>ReserveChoiceId</c> runs. Host-only or order-sensitive UI (rest site, YGO smith, card grids, etc.) can advance
/// the host’s counters more than the client’s while gameplay state still matches — vanilla includes those uints in
/// <see cref="ChecksumTracker"/>’s xxhash and MP disconnects. For Host/Client only, omit them from the hashed bytes.
/// Singleplayer and Replay leave the snapshot unchanged so replay checksum verification is unaffected.
/// </summary>
[HarmonyPatch(typeof(ChecksumTracker), nameof(ChecksumTracker.GenerateChecksum), typeof(NetFullCombatState))]
public static class ChecksumTrackerMpStripEphemeralChoiceIdsPatch
{
    [HarmonyPrefix]
    public static void Prefix(ChecksumTracker __instance, NetFullCombatState state)
    {
        if (state.nextChoiceIds is not { Count: > 0 })
            return;

        NetGameType netType = Traverse.Create(__instance).Field<INetGameService>("_netService").Value?.Type
            ?? NetGameType.None;
        if (netType != NetGameType.Host && netType != NetGameType.Client)
            return;

        int n = state.nextChoiceIds.Count;
        state.nextChoiceIds = new List<uint>();

        if (NetFullCombatStateYgoChecksumPatch.VerboseChecksumLog)
            GD.Print($"[YgoDuelist][MP][Checksum] omitted {n} choice-id slot(s) from MP checksum hash (net={netType})");
    }
}
