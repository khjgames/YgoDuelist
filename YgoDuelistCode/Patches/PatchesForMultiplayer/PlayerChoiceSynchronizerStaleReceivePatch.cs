using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Models;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Vanilla <see cref="PlayerChoiceSynchronizer"/> buffers <see cref="PlayerChoiceMessage"/> results when they arrive
/// before <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/> runs. If the local per-player choice counter
/// advances past that id without consuming the buffer (host/client executed a different number of
/// <c>ReserveChoiceId</c> calls, or a message arrived too late), a stale buffered result can satisfy a later wait for
/// the same numeric id and desync combat state.
/// </summary>
[HarmonyPatch(typeof(PlayerChoiceSynchronizer), "OnReceivePlayerChoice", typeof(Player), typeof(uint), typeof(NetPlayerChoiceResult))]
public static class PlayerChoiceSynchronizerStaleReceivePatch
{
    /// <summary>Log every incoming remote choice (indexes) before filtering; very noisy.</summary>
    public static bool VerboseReceiveLog;

    /// <summary>Matches vanilla private <c>GetChoiceId</c>: next id that <see cref="PlayerChoiceSynchronizer.ReserveChoiceId"/> will assign.</summary>
    public static uint GetNextChoiceId(PlayerChoiceSynchronizer sync, Player player)
    {
        var players = Traverse.Create(sync).Field<IPlayerCollection>("_players").Value;
        int slot = players.GetPlayerSlotIndex(player);
        IReadOnlyList<uint> ids = sync.ChoiceIds;
        if (slot >= ids.Count)
            return 0u;
        return ids[slot];
    }

    [HarmonyPrefix]
    public static bool Prefix(PlayerChoiceSynchronizer __instance, Player player, uint choiceId, NetPlayerChoiceResult result)
    {
        NetGameType net = RunManager.Instance.NetService.Type;
        if (net != NetGameType.Host && net != NetGameType.Client)
            return true;

        uint next = GetNextChoiceId(__instance, player);
        if (VerboseReceiveLog)
            GD.Print($"[YgoDuelist][MP][PlayerChoice] recv prefix choiceId={choiceId} localNext={next} sender={player.NetId} result={result}");

        GridCombatMpExpectation.Active? gridExp = GridCombatMpExpectation.Pending.Value;
        if (gridExp.HasValue
            && player.NetId == gridExp.Value.OwnerNetId
            && result.type == PlayerChoiceType.Index
            && result.indexes != null)
        {
            int idxCount = result.indexes.Count;
            if (idxCount < gridExp.Value.MinSelect || idxCount > gridExp.Value.MaxSelect)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][PlayerChoice] Dropping remote Index (count={idxCount} expected [{gridExp.Value.MinSelect},{gridExp.Value.MaxSelect}]) choiceId={choiceId} sender={player.NetId}");
                return false;
            }
        }

        // Valid waits use choiceId where choiceId < next after the matching Reserve (next == choiceId + 1).
        // If next is already > choiceId + 1, we have moved past that id; buffering would be stale.
        if (next > choiceId + 1)
        {
            GD.PrintErr(
                $"[YgoDuelist][MP][PlayerChoice] Dropping stale remote choice (would corrupt later Wait): choiceId={choiceId} localNextChoiceId={next} sender={player.NetId} result={result}");
            return false;
        }

        return true;
    }
}

/// <summary>Optional MP tracing for <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/> vs local counters.</summary>
[HarmonyPatch(typeof(PlayerChoiceSynchronizer), nameof(PlayerChoiceSynchronizer.WaitForRemoteChoice))]
public static class PlayerChoiceSynchronizerWaitRemoteLogPatch
{
    public static bool VerboseWaitLog;

    [HarmonyPrefix]
    public static void Prefix(PlayerChoiceSynchronizer __instance, Player player, uint choiceId)
    {
        if (!VerboseWaitLog)
            return;
        NetGameType net = RunManager.Instance.NetService.Type;
        if (net != NetGameType.Host && net != NetGameType.Client)
            return;
        uint next = PlayerChoiceSynchronizerStaleReceivePatch.GetNextChoiceId(__instance, player);
        GD.Print($"[YgoDuelist][MP][PlayerChoice] WaitForRemoteChoice begin choiceId={choiceId} localNext={next} ownerNet={player.NetId}");
    }
}

/// <summary>Trace <see cref="PlayerChoiceSynchronizer.ReserveChoiceId"/> returned ids (compare host vs client logs).</summary>
[HarmonyPatch(typeof(PlayerChoiceSynchronizer), nameof(PlayerChoiceSynchronizer.ReserveChoiceId))]
public static class PlayerChoiceSynchronizerReserveLogPatch
{
    public static bool VerboseReserveLog;

    [HarmonyPostfix]
    public static void Postfix(PlayerChoiceSynchronizer __instance, Player player, uint __result)
    {
        if (!VerboseReserveLog)
            return;
        NetGameType net = RunManager.Instance.NetService.Type;
        if (net != NetGameType.Host && net != NetGameType.Client)
            return;
        uint next = PlayerChoiceSynchronizerStaleReceivePatch.GetNextChoiceId(__instance, player);
        GD.Print($"[YgoDuelist][MP][PlayerChoice] ReserveChoiceId returned={__result} nextAfter={next} playerNet={player.NetId}");
    }
}

/// <summary>
/// Pre-buffered <see cref="PlayerChoiceType.Index"/> results are fulfilled immediately in
/// <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/>; drop entries whose index count does not match the active
/// YGO combat grid expectation (same window as <see cref="GridCombatMpExpectation"/>).
/// </summary>
[HarmonyPatch(typeof(PlayerChoiceSynchronizer), nameof(PlayerChoiceSynchronizer.WaitForRemoteChoice))]
[HarmonyPriority(Priority.First)]
public static class PlayerChoiceSynchronizerDiscardInvalidBufferedGridIndexPatch
{
    [HarmonyPrefix]
    public static void Prefix(PlayerChoiceSynchronizer __instance, Player player, uint choiceId)
    {
        GridCombatMpExpectation.Active? exp = GridCombatMpExpectation.Pending.Value;
        if (!exp.HasValue || player.NetId != exp.Value.OwnerNetId)
            return;

        object? listObj = Traverse.Create(__instance).Field("_receivedChoices").GetValue();
        if (listObj is not IList list)
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            object item = list[i]!;
            uint cId = Traverse.Create(item).Field<uint>("choiceId").Value;
            ulong senderId = Traverse.Create(item).Field<ulong>("senderId").Value;
            if (cId != choiceId || senderId != player.NetId)
                continue;

            object? tcsObj = Traverse.Create(item).Field("completionSource").GetValue();
            if (tcsObj == null)
                continue;

            var taskProp = tcsObj.GetType().GetProperty("Task");
            if (taskProp?.GetValue(tcsObj) is not Task<NetPlayerChoiceResult> task || !task.IsCompleted)
                continue;

            NetPlayerChoiceResult net = task.Result;
            if (net.type != PlayerChoiceType.Index || net.indexes == null)
                continue;

            int count = net.indexes.Count;
            if (count >= exp.Value.MinSelect && count <= exp.Value.MaxSelect)
                continue;

            list.RemoveAt(i);
            GD.PrintErr(
                $"[YgoDuelist][MP][PlayerChoice] Removed invalid pre-buffered Index (count={count} expected [{exp.Value.MinSelect},{exp.Value.MaxSelect}]) choiceId={choiceId} sender={player.NetId}");
        }
    }
}
