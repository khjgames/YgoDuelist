using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Models;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO multi-select grids with MP sync. Cancellations sync so <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/> completes.
/// <para>
/// <b>Combat-card wire</b> (<see cref="FromSimpleGridCombat"/>): same as <see cref="CardSelectCmd.FromHand"/> — serializes
/// <see cref="NetCombatCard"/> identities. Use for hand / ritual material grids so peers agree even when
/// <see cref="CardPile.Cards"/> order or pile membership (e.g. spell in hand vs spell/trap zone row) differs.
/// </para>
/// <para>
/// <b>Index wire</b> (<see cref="FromSimpleGridIndexed"/>): for tribute grids that include synthetic entries such as
/// <see cref="IYgoMausoleumHpTributeOption"/> rows which are not stable across the combat-card net path. Remote peers rebuild the canonical
/// candidate list then apply indexes.
/// </para>
/// </summary>
public static class TributeSummonGridSelect
{
    /// <summary>Set true to log synced grid indexes (indexed MP path).</summary>
    public static bool VerboseMpLog;

    /// <summary>
    /// When true, logs stabilized combat-card ids for each <see cref="BuildStabilizedHandCandidates"/> call (MP debugging).
    /// </summary>
    public static bool VerboseHandGridMpLog;

    /// <summary>
    /// <see cref="CardPile.Cards"/> order is not guaranteed to match across MP peers (layout / iteration). Sort by
    /// <see cref="NetCombatCardDb"/> ids so grid indexes refer to the same cards on host and observers.
    /// </summary>
    public static List<CardModel> StabilizeHandPileCandidates(IEnumerable<CardModel> cards)
    {
        List<CardModel> list = cards.ToList();
        YgoNetCombatCardPileGate.EnsureMutableCombatCardsHaveNetIds(list);
        return list.OrderBy(c => NetCombatCardDb.Instance.GetCardId(c)).ToList();
    }

    /// <summary>
    /// Hand cards for <see cref="FromSimpleGridCombat"/>: optional filter, optional exclusion (e.g. the card being played), then stabilize.
    /// </summary>
    public static List<CardModel> BuildStabilizedHandCandidates(
        Player player,
        Func<CardModel, bool>? predicate,
        CardModel? excludeReference = null)
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return new List<CardModel>();

        IEnumerable<CardModel> q = hand.Cards;
        if (excludeReference != null)
            q = q.Where(c => !ReferenceEquals(c, excludeReference));
        if (predicate != null)
            q = q.Where(predicate);

        List<CardModel> list = StabilizeHandPileCandidates(q);

        if (VerboseHandGridMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            string ids = string.Join(",", list.Select(c => NetCombatCardDb.Instance.GetCardId(c)));
            GD.Print($"[YgoDuelist][MP][HandGrid] BuildStabilizedHandCandidates owner={player.NetId} count={list.Count} netIds=[{ids}]");
        }

        return list;
    }

    private static bool ShouldSelectLocalCard(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

    /// <summary>
    /// Hand / ritual-style grids: MP sync prefers <see cref="PlayerChoiceResult.FromMutableCombatCards"/> (same contract as
    /// <see cref="CardSelectCmd.FromHand"/>). If the synchronizer delivers a pre-buffered <see cref="PlayerChoiceType.Index"/>
    /// result for this choice id (ordering / duplicate reservation), remote peers apply indexes into
    /// <paramref name="rebuildCanonicalForRemoteApply"/> (or the snapshot list) so execution does not throw.
    /// </summary>
    /// <param name="rebuildCanonicalForRemoteApply">
    /// Same rules as the local grid’s candidate list after any state change (e.g. after draw). Used when the remote result is Index.
    /// </param>
    public static async Task<IEnumerable<CardModel>> FromSimpleGridCombat(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cardsIn,
        Player player,
        CardSelectorPrefs prefs,
        Func<List<CardModel>>? rebuildCanonicalForRemoteApply = null,
        PlayerChoiceOptions choiceBegunOptions = PlayerChoiceOptions.None)
    {
        if (CombatManager.Instance.IsEnding)
            return Array.Empty<CardModel>();

        List<CardModel> cards = cardsIn.ToList();
        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
            return cards.ToList();

        bool localSelect = ShouldSelectLocalCard(player);
        NetGameType net = RunManager.Instance.NetService.Type;
        bool mpObserver = !localSelect && (net == NetGameType.Host || net == NetGameType.Client);
        IDisposable? expectationScope = null;
        if (mpObserver)
        {
            expectationScope = GridCombatMpExpectation.Push(new GridCombatMpExpectation.Active
            {
                OwnerNetId = player.NetId,
                MinSelect = prefs.Cancelable ? 0 : prefs.MinSelect,
                MaxSelect = prefs.MaxSelect,
                CandidateRowCount = cards.Count,
                AllowCombatCard = true
            });
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);

        try
        {
            await context.SignalPlayerChoiceBegun(choiceBegunOptions);
            try
            {
                List<CardModel> result;
                if (localSelect)
                {
                    result = await SelectLocalGridResultsAsync(cards, prefs, player, choiceId, syncCancelAsCombatWire: true);

                    if (VerboseMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                    {
                        string ids = string.Join(",", result.Select(c => NetCombatCardDb.Instance.GetCardId(c)));
                        string preview = string.Join("|", result.Select(c => $"{c.Id?.Entry}"));
                        GD.Print($"[YgoDuelist][MP][GridCombat] SyncLocalChoice choiceId={choiceId} ownerNet={player.NetId} netIds=[{ids}] picked={preview}");
                    }

                    RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                        player,
                        choiceId,
                        PlayerChoiceResult.FromMutableCombatCards(result));
                }
                else
                {
                    PlayerChoiceResult remoteResult =
                        await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId);

                    if (remoteResult.ChoiceType == PlayerChoiceType.CombatCard)
                    {
                        result = remoteResult.AsCombatCards().ToList();
                        if (VerboseMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                        {
                            string ids = string.Join(",", result.Select(c => NetCombatCardDb.Instance.GetCardId(c)));
                            GD.Print($"[YgoDuelist][MP][GridCombat] ApplyRemote combat wire choiceId={choiceId} ownerNet={player.NetId} netIds=[{ids}]");
                        }
                    }
                    else if (remoteResult.ChoiceType == PlayerChoiceType.Index)
                    {
                        List<int> remoteIndexes = remoteResult.AsIndexes().ToList();
                        if (remoteIndexes.Count < prefs.MinSelect || remoteIndexes.Count > prefs.MaxSelect)
                        {
                            throw new InvalidOperationException(
                                $"[YgoDuelist][MP][GridCombat] Remote Index count {remoteIndexes.Count} outside [{prefs.MinSelect},{prefs.MaxSelect}] choiceId={choiceId} ownerNet={player.NetId}.");
                        }

                        List<CardModel> resolve = rebuildCanonicalForRemoteApply != null
                            ? rebuildCanonicalForRemoteApply()
                            : cards;
                        GD.PrintErr(
                            $"[YgoDuelist][MP][GridCombat] Remote choice was Index (not combat wire) choiceId={choiceId} ownerNet={player.NetId} indexes=[{string.Join(",", remoteIndexes)}] resolveCount={resolve.Count} — applying via rebuild/snapshot.");
                        if (VerboseHandGridMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                        {
                            string r = string.Join(",", resolve.Select(c => NetCombatCardDb.Instance.GetCardId(c)));
                            GD.Print($"[YgoDuelist][MP][GridCombat] Index path resolve netIds=[{r}]");
                        }

                        if (remoteIndexes.Any(i => i < 0 || i >= resolve.Count))
                        {
                            LogTributeGridMismatch("GridCombat_remote_index_oob", resolve, remoteIndexes, player);
                            throw new InvalidOperationException(
                                "[YgoDuelist][MP][GridCombat] Remote indexes out of range for combat grid.");
                        }

                        result = remoteIndexes.Select(i => resolve[i]).ToList();
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"[YgoDuelist][MP][GridCombat] Unexpected PlayerChoiceType {remoteResult.ChoiceType} for choiceId={choiceId} ownerNet={player.NetId}");
                    }
                }

                return result;
            }
            finally
            {
                await context.SignalPlayerChoiceEnded();
            }
        }
        finally
        {
            expectationScope?.Dispose();
        }
    }

    /// <param name="rebuildCanonicalForRemoteApply">
    /// Non-local peers apply host indexes into the candidate list. Rebuild with the same rules as the choosing player
    /// immediately before applying indexes (field tribute with Mausoleum placeholders, etc.).
    /// </param>
    public static async Task<IEnumerable<CardModel>> FromSimpleGridIndexed(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cardsIn,
        Player player,
        CardSelectorPrefs prefs,
        Func<List<CardModel>>? rebuildCanonicalForRemoteApply = null,
        PlayerChoiceOptions choiceBegunOptions = PlayerChoiceOptions.None)
    {
        if (CombatManager.Instance.IsEnding)
            return Array.Empty<CardModel>();

        List<CardModel> cards = cardsIn.ToList();
        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
            return cards.ToList();

        NetGameType net = RunManager.Instance.NetService.Type;
        bool localSelect = ShouldSelectLocalCard(player);
        bool mpObserver = !localSelect && (net == NetGameType.Host || net == NetGameType.Client);
        IDisposable? expectationScope = null;
        if (mpObserver)
        {
            expectationScope = GridCombatMpExpectation.Push(new GridCombatMpExpectation.Active
            {
                OwnerNetId = player.NetId,
                MinSelect = prefs.Cancelable ? 0 : prefs.MinSelect,
                MaxSelect = prefs.MaxSelect,
                CandidateRowCount = cards.Count,
                AllowCombatCard = false,
                AllowIndex = true
            });
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);

        await context.SignalPlayerChoiceBegun(choiceBegunOptions);
        try
        {
            List<CardModel> result;
            if (localSelect)
            {
                result = await SelectLocalGridResultsAsync(cards, prefs, player, choiceId, syncCancelAsCombatWire: false);

                List<int> indexes = result.Select(c => cards.IndexOf(c)).ToList();
                if (indexes.Any(i => i < 0))
                {
                    LogTributeGridMismatch("host_IndexOf_missing", cards, indexes, player);
                    throw new InvalidOperationException(
                        "[YgoDuelist][MP][Tribute] Selected card not in canonical grid list (IndexOf=-1).");
                }

                if (VerboseMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                {
                    string idx = string.Join(",", indexes);
                    string preview = string.Join("|", cards.Select((c, i) => $"{i}:{c.Id?.Entry}"));
                    GD.Print($"[YgoDuelist][MP][GridIndexed] SyncLocalChoice choiceId={choiceId} ownerNet={player.NetId} indexes=[{idx}] candidates={preview}");
                }

                RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndexes(indexes));
            }
            else
            {
                List<int> remoteIndexes = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId))
                    .AsIndexes()
                    .ToList();
                List<CardModel> resolve = rebuildCanonicalForRemoteApply != null
                    ? rebuildCanonicalForRemoteApply()
                    : cards;
                if (VerboseHandGridMpLog && rebuildCanonicalForRemoteApply != null
                    && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                {
                    string r = string.Join(",", resolve.Select(c => NetCombatCardDb.Instance.GetCardId(c)));
                    GD.Print(
                        $"[YgoDuelist][MP][HandGrid] ApplyRemoteRebuild choiceId={choiceId} ownerNet={player.NetId} resolveCount={resolve.Count} netIds=[{r}]");
                }

                if (remoteIndexes.Any(i => i < 0 || i >= resolve.Count))
                {
                    LogTributeGridMismatch("remote_index_oob", resolve, remoteIndexes, player);
                    throw new InvalidOperationException(
                        $"[YgoDuelist][MP][Tribute] Remote tribute indexes out of range (count={resolve.Count}).");
                }

                if (VerboseMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
                {
                    string idx = string.Join(",", remoteIndexes);
                    string preview = string.Join("|", resolve.Select((c, i) => $"{i}:{c.Id?.Entry}"));
                    GD.Print($"[YgoDuelist][MP][GridIndexed] ApplyRemote choiceId={choiceId} ownerNet={player.NetId} indexes=[{idx}] resolve={preview}");
                }

                result = remoteIndexes.Select(i => resolve[i]).ToList();
            }

            return result;
        }
        finally
        {
            try
            {
                await context.SignalPlayerChoiceEnded();
            }
            finally
            {
                expectationScope?.Dispose();
            }
        }
    }

    private static async Task<List<CardModel>> SelectLocalGridResultsAsync(
        List<CardModel> cards,
        CardSelectorPrefs prefs,
        Player player,
        uint choiceId,
        bool syncCancelAsCombatWire)
    {
        if (CardSelectCmd.Selector != null)
        {
            try
            {
                return (await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect)).ToList();
            }
            catch (OperationCanceledException)
            {
                SyncCancelIfMp(player, choiceId, syncCancelAsCombatWire);
                throw;
            }
        }

        // Do not call NPlayerHand.CancelAllCardPlay() here. During a hand play (e.g. tribute summon) the parent
        // PlayCardAction is still active; CancelAllCardPlay can abort the nested grid await, fire
        // OperationCanceledException, and SyncCancelIfMp → empty combat-card wire for the reserved choice id — host
        // and observer then diverge (spurious "Grid cancel → empty" at the tribute id; second attempt uses shifted ids).
        NSimpleCardSelectScreen screen = NSimpleCardSelectScreen.Create(cards, prefs);
        NOverlayStack.Instance.Push(screen);
        try
        {
            return (await screen.CardsSelected()).ToList();
        }
        catch (OperationCanceledException)
        {
            SyncCancelIfMp(player, choiceId, syncCancelAsCombatWire);
            throw;
        }
    }

    private static void LogTributeGridMismatch(string tag, List<CardModel> resolve, List<int> indexes, Player player)
    {
        var lines = new List<string>
        {
            $"[YgoDuelist][MP][Tribute][{tag}] ownerNet={player.NetId} resolveCount={resolve.Count} indexes={string.Join(",", indexes)}"
        };
        for (int i = 0; i < resolve.Count; i++)
        {
            CardModel c = resolve[i];
            string extra = c is IYgoMausoleumHpTributeOption m ? $" slot={m.MausoleumGridSlot}" : "";
            lines.Add($"  [{i}] {c.Id?.Entry}{extra}");
        }

        GD.PrintErr(string.Join("\n", lines));
    }

    private static void SyncCancelIfMp(Player player, uint choiceId, bool syncCancelAsCombatWire)
    {
        if (!LocalContext.IsMe(player))
            return;
        if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            return;

        if (syncCancelAsCombatWire)
        {
            GD.Print($"[YgoDuelist][MP][GridCombat] Grid cancel → empty combat cards choiceId={choiceId} ownerNet={player.NetId}");
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player,
                choiceId,
                PlayerChoiceResult.FromMutableCombatCards(new List<CardModel>()));
        }
        else
        {
            GD.Print($"[YgoDuelist][MP][GridIndexed] Grid cancel → empty indexes choiceId={choiceId} ownerNet={player.NetId}");
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player,
                choiceId,
                PlayerChoiceResult.FromIndexes(new List<int>()));
        }
    }
}
