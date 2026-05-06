using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Models;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
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
    private static readonly Dictionary<ulong, GridCombatMpExpectation.Active> ActiveRemoteWaits = new();
    private static readonly object ActiveRemoteWaitsLock = new();

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

    public static string? GetExpectationRejectReason(NetPlayerChoiceResult result, GridCombatMpExpectation.Active exp)
    {
        switch (result.type)
        {
            case PlayerChoiceType.Index:
                if (!exp.AllowIndex)
                    return "Index wire is not allowed";
                if (result.indexes == null)
                    return "Index wire had null indexes";
                int idxCount = result.indexes.Count;
                if (idxCount < exp.MinSelect || idxCount > exp.MaxSelect)
                    return $"Index count={idxCount} outside [{exp.MinSelect},{exp.MaxSelect}]";
                int rows = exp.CandidateRowCount;
                if (rows > 0 && result.indexes.Any(ix => ix < 0 && !exp.AllowNegativeIndex || ix >= rows))
                    return $"Index out of bounds for rows={rows} indexes={string.Join(",", result.indexes)}";
                return null;

            case PlayerChoiceType.CombatCard:
                if (!exp.AllowCombatCard)
                    return "CombatCard wire is not allowed";
                int combatCount = result.combatCards?.Count ?? 0;
                return combatCount < exp.MinSelect || combatCount > exp.MaxSelect
                    ? $"CombatCard count={combatCount} outside [{exp.MinSelect},{exp.MaxSelect}]"
                    : null;
            case PlayerChoiceType.DeckCard:
                if (!exp.AllowDeckCard)
                    return "DeckCard wire is not allowed";
                int deckCount = result.deckCards?.Count ?? 0;
                return deckCount < exp.MinSelect || deckCount > exp.MaxSelect
                    ? $"DeckCard count={deckCount} outside [{exp.MinSelect},{exp.MaxSelect}]"
                    : null;
            case PlayerChoiceType.CanonicalCard:
                if (!exp.AllowCanonicalCard)
                    return "CanonicalCard wire is not allowed";
                int canonicalCount = result.canonicalCards?.Count ?? 0;
                return canonicalCount < exp.MinSelect || canonicalCount > exp.MaxSelect
                    ? $"CanonicalCard count={canonicalCount} outside [{exp.MinSelect},{exp.MaxSelect}]"
                    : null;
            case PlayerChoiceType.MutableCard:
                if (!exp.AllowMutableCard)
                    return "MutableCard wire is not allowed";
                int mutableCount = result.mutableCards?.Count ?? 0;
                return mutableCount < exp.MinSelect || mutableCount > exp.MaxSelect
                    ? $"MutableCard count={mutableCount} outside [{exp.MinSelect},{exp.MaxSelect}]"
                    : null;
            case PlayerChoiceType.Player:
                return exp.AllowPlayer ? null : "Player wire is not allowed";
            default:
                return $"{result.type} wire is not allowed";
        }
    }

    public static string DescribeExpectation(GridCombatMpExpectation.Active exp) =>
        $"expectedChoiceId={exp.ExpectedChoiceId?.ToString() ?? "?"} allowCombat={exp.AllowCombatCard} allowIndex={exp.AllowIndex} allowDeck={exp.AllowDeckCard} allowCanonical={exp.AllowCanonicalCard} allowMutable={exp.AllowMutableCard} allowPlayer={exp.AllowPlayer} min={exp.MinSelect} max={exp.MaxSelect} rows={exp.CandidateRowCount} allowNegativeIndex={exp.AllowNegativeIndex}";

    public static void TrackActiveRemoteWait(ulong ownerNetId, GridCombatMpExpectation.Active exp)
    {
        if (exp.ExpectedChoiceId == null)
            return;

        lock (ActiveRemoteWaitsLock)
        {
            ActiveRemoteWaits[ownerNetId] = exp;
        }

        GD.Print(
            $"[YgoDuelist][MP][PlayerChoice] Tracking active remote wait ownerNet={ownerNetId}; {DescribeExpectation(exp)}");
    }

    public static void ClearActiveRemoteWait(ulong ownerNetId, uint choiceId)
    {
        lock (ActiveRemoteWaitsLock)
        {
            if (!ActiveRemoteWaits.TryGetValue(ownerNetId, out GridCombatMpExpectation.Active exp)
                || exp.ExpectedChoiceId != choiceId)
                return;

            ActiveRemoteWaits.Remove(ownerNetId);
        }

        GD.Print(
            $"[YgoDuelist][MP][PlayerChoice] Cleared active remote wait ownerNet={ownerNetId} choiceId={choiceId}");
    }

    private static GridCombatMpExpectation.Active? GetActiveRemoteWait(ulong ownerNetId)
    {
        lock (ActiveRemoteWaitsLock)
        {
            return ActiveRemoteWaits.TryGetValue(ownerNetId, out GridCombatMpExpectation.Active exp)
                ? exp
                : null;
        }
    }

    [HarmonyPrefix]
    public static bool Prefix(PlayerChoiceSynchronizer __instance, Player player, ref uint choiceId, NetPlayerChoiceResult result)
    {
        NetGameType net = RunManager.Instance.NetService.Type;
        if (net != NetGameType.Host && net != NetGameType.Client)
            return true;

        uint next = GetNextChoiceId(__instance, player);
        if (VerboseReceiveLog)
            GD.Print($"[YgoDuelist][MP][PlayerChoice] recv prefix choiceId={choiceId} localNext={next} sender={player.NetId} result={result}");

        GridCombatMpExpectation.Active? exp = GridCombatMpExpectation.Pending.Value;
        if (!exp.HasValue || player.NetId != exp.Value.OwnerNetId)
            exp = GetActiveRemoteWait(player.NetId);

        if (exp.HasValue && player.NetId == exp.Value.OwnerNetId)
        {
            string? rejectReason = GetExpectationRejectReason(result, exp.Value);
            if (rejectReason != null)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][PlayerChoice] Dropping remote {result.type} while waiting for active choice choiceId={choiceId} sender={player.NetId}: {rejectReason}; {DescribeExpectation(exp.Value)}");
                return false;
            }

            if (exp.Value.ExpectedChoiceId is uint expectedChoiceId && choiceId != expectedChoiceId)
            {
                if (choiceId < expectedChoiceId)
                {
                    GD.PrintErr(
                        $"[YgoDuelist][MP][PlayerChoice] Dropping stale valid-looking remote {result.type}: incoming choiceId={choiceId} is older than active expectedChoiceId={expectedChoiceId} sender={player.NetId}; {DescribeExpectation(exp.Value)} result={result}");
                    return false;
                }

                GD.PrintErr(
                    $"[YgoDuelist][MP][PlayerChoice] Remapping valid future remote {result.type} to active wait: incoming choiceId={choiceId} expectedChoiceId={expectedChoiceId} sender={player.NetId}; {DescribeExpectation(exp.Value)} result={result}");
                choiceId = expectedChoiceId;
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
        NetGameType net = RunManager.Instance.NetService.Type;
        GridCombatMpExpectation.Active? exp = GridCombatMpExpectation.Pending.Value;
        if ((net == NetGameType.Host || net == NetGameType.Client)
            && exp.HasValue
            && exp.Value.OwnerNetId == player.NetId
            && exp.Value.ExpectedChoiceId == null)
        {
            GridCombatMpExpectation.Pending.Value = GridCombatMpExpectation.WithExpectedChoiceId(exp.Value, __result);
            GD.Print(
                $"[YgoDuelist][MP][PlayerChoice] Bound active choice expectation to choiceId={__result} ownerNet={player.NetId}; {PlayerChoiceSynchronizerStaleReceivePatch.DescribeExpectation(GridCombatMpExpectation.Pending.Value.Value)}");
        }

        if (!VerboseReserveLog)
            return;
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
    /// <summary>
    /// When several late-arriving pre-buffered results were remapped to the same <paramref name="choiceId"/>,
    /// <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/> uses <c>FindIndex</c> on the first match. A stale
    /// empty <see cref="PlayerChoiceType.CombatCard"/> remap can otherwise win over the host's real non-empty tribute
    /// pick (checksum drift after <see cref="PlayCardActionTributeSelectionPatch"/>).
    /// </summary>
    private static int CompletedChoiceWirePayloadScore(NetPlayerChoiceResult net) =>
        net.type switch
        {
            PlayerChoiceType.CombatCard => net.combatCards?.Count ?? 0,
            PlayerChoiceType.Index => net.indexes?.Count ?? 0,
            PlayerChoiceType.DeckCard => net.deckCards?.Count ?? 0,
            PlayerChoiceType.CanonicalCard => net.canonicalCards?.Count ?? 0,
            PlayerChoiceType.MutableCard => net.mutableCards?.Count ?? 0,
            _ => 0
        };

    private static void DedupeCompletedBufferedChoicesForSenderAndId(IList list, ulong ownerNetId, uint choiceId)
    {
        var scored = new List<(int listIndex, int score)>();
        for (int i = 0; i < list.Count; i++)
        {
            object item = list[i]!;
            uint cId = Traverse.Create(item).Field<uint>("choiceId").Value;
            ulong senderId = Traverse.Create(item).Field<ulong>("senderId").Value;
            if (senderId != ownerNetId || cId != choiceId)
                continue;

            object? tcsObj = Traverse.Create(item).Field("completionSource").GetValue();
            if (tcsObj == null)
                continue;

            var taskProp = tcsObj.GetType().GetProperty("Task");
            if (taskProp?.GetValue(tcsObj) is not Task<NetPlayerChoiceResult> task || !task.IsCompleted)
                continue;

            NetPlayerChoiceResult net = task.Result;
            scored.Add((i, CompletedChoiceWirePayloadScore(net)));
        }

        if (scored.Count <= 1)
            return;

        int keepListIndex = scored[0].listIndex;
        int keepScore = scored[0].score;
        for (int s = 1; s < scored.Count; s++)
        {
            int idx = scored[s].listIndex;
            int sc = scored[s].score;
            if (sc > keepScore || (sc == keepScore && idx > keepListIndex))
            {
                keepListIndex = idx;
                keepScore = sc;
            }
        }

        foreach (int removeAt in scored
                     .Where(t => t.listIndex != keepListIndex)
                     .Select(t => t.listIndex)
                     .OrderByDescending(i => i))
        {
            list.RemoveAt(removeAt);
        }

        GD.PrintErr(
            $"[YgoDuelist][MP][PlayerChoice] Deduped {scored.Count} pre-buffered results for choiceId={choiceId} ownerNet={ownerNetId}; kept listIndex={keepListIndex} score={keepScore}");
    }

    [HarmonyPrefix]
    public static void Prefix(PlayerChoiceSynchronizer __instance, Player player, uint choiceId)
    {
        GridCombatMpExpectation.Active? exp = GridCombatMpExpectation.Pending.Value;
        if (!exp.HasValue || player.NetId != exp.Value.OwnerNetId)
            return;

        if (exp.Value.ExpectedChoiceId == null)
        {
            GridCombatMpExpectation.Pending.Value = GridCombatMpExpectation.WithExpectedChoiceId(exp.Value, choiceId);
            exp = GridCombatMpExpectation.Pending.Value;
            GD.Print(
                $"[YgoDuelist][MP][PlayerChoice] Bound active choice expectation at WaitForRemoteChoice choiceId={choiceId} ownerNet={player.NetId}; {PlayerChoiceSynchronizerStaleReceivePatch.DescribeExpectation(exp.Value)}");
        }

        PlayerChoiceSynchronizerStaleReceivePatch.TrackActiveRemoteWait(player.NetId, exp.Value);

        object? listObj = Traverse.Create(__instance).Field("_receivedChoices").GetValue();
        if (listObj is not IList list)
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            object item = list[i]!;
            uint cId = Traverse.Create(item).Field<uint>("choiceId").Value;
            ulong senderId = Traverse.Create(item).Field<ulong>("senderId").Value;
            if (senderId != player.NetId)
                continue;

            object? tcsObj = Traverse.Create(item).Field("completionSource").GetValue();
            if (tcsObj == null)
                continue;

            var taskProp = tcsObj.GetType().GetProperty("Task");
            if (taskProp?.GetValue(tcsObj) is not Task<NetPlayerChoiceResult> task || !task.IsCompleted)
                continue;

            NetPlayerChoiceResult net = task.Result;
            string? rejectReason = PlayerChoiceSynchronizerStaleReceivePatch.GetExpectationRejectReason(net, exp.Value);
            if (cId != choiceId)
            {
                if (exp.Value.ExpectedChoiceId is not uint expectedChoiceId || choiceId != expectedChoiceId)
                    continue;

                if (cId < expectedChoiceId)
                {
                    list.RemoveAt(i);
                    GD.PrintErr(
                        $"[YgoDuelist][MP][PlayerChoice] Removed older pre-buffered {net.type} choiceId={cId} while active wait expects {expectedChoiceId} sender={player.NetId}; {PlayerChoiceSynchronizerStaleReceivePatch.DescribeExpectation(exp.Value)}");
                    continue;
                }

                if (rejectReason == null)
                {
                    // Future-id empty CombatCard buffers are almost always unrelated cancels; remapping them onto a
                    // new tribute/combat wait satisfies WaitForRemoteChoice with zero picks while the host confirmed
                    // materials (PlayCardAction checksum after Labyrinth Wall–style summons).
                    if (cId > expectedChoiceId && net.type == PlayerChoiceType.CombatCard
                        && (net.combatCards == null || net.combatCards.Count == 0))
                    {
                        list.RemoveAt(i);
                        GD.PrintErr(
                            $"[YgoDuelist][MP][PlayerChoice] Removed empty pre-buffered future CombatCard (do not remap to active wait) choiceId={cId}→{choiceId} sender={player.NetId}; {PlayerChoiceSynchronizerStaleReceivePatch.DescribeExpectation(exp.Value)}");
                        continue;
                    }

                    var itemTraverse = Traverse.Create(item);
                    itemTraverse.Field<uint>("choiceId").Value = choiceId;
                    list[i] = itemTraverse.GetValue();
                    GD.PrintErr(
                        $"[YgoDuelist][MP][PlayerChoice] Remapped valid pre-buffered future {net.type} choiceId={cId} to active wait choiceId={choiceId} sender={player.NetId}; {PlayerChoiceSynchronizerStaleReceivePatch.DescribeExpectation(exp.Value)}");
                    continue;
                }
            }

            if (rejectReason == null)
            {
                continue;
            }

            list.RemoveAt(i);
            GD.PrintErr(
                $"[YgoDuelist][MP][PlayerChoice] Removed invalid pre-buffered {net.type} for active choice choiceId={choiceId} sender={player.NetId}: {rejectReason}; {PlayerChoiceSynchronizerStaleReceivePatch.DescribeExpectation(exp.Value)}");
        }

        DedupeCompletedBufferedChoicesForSenderAndId(list, player.NetId, choiceId);
    }

    [HarmonyPostfix]
    public static void Postfix(Player player, uint choiceId, Task<PlayerChoiceResult> __result)
    {
        _ = __result.ContinueWith(
            _ => PlayerChoiceSynchronizerStaleReceivePatch.ClearActiveRemoteWait(player.NetId, choiceId),
            TaskScheduler.Default);
    }
}
