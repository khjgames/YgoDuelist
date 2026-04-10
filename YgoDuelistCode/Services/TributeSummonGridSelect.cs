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
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tribute material grid and other YGO multi-selects: same behavior as <see cref="CardSelectCmd.FromSimpleGrid"/>, but when the local
/// summoning player cancels the overlay, sends an empty <see cref="PlayerChoiceResult"/> for that choice id. Vanilla
/// <see cref="CardSelectCmd"/> does not sync on cancel, so <see cref="PlayerChoiceSynchronizer.WaitForRemoteChoice"/>
/// never completes on other peers and the next grid uses a different id than observers — tribute summon MP breaks.
    /// <para>
    /// Uses <see cref="PlayerChoiceResult.FromIndexes"/> for MP (not <see cref="CardSelectCmd.FromHand"/>'s combat-card wire),
    /// so observers never call <see cref="PlayerChoiceResult.AsCombatCards"/> on a result that arrived as indexes.
    /// </para>
    /// </summary>
public static class TributeSummonGridSelect
{
    /// <summary>Set true to log synced grid indexes (MP: must match remote reconstruction from the same candidate ordering).</summary>
    public static bool VerboseMpLog;

    /// <summary>
    /// When true, logs stabilized combat-card ids for each <see cref="BuildStabilizedHandCandidates"/> call (MP debugging).
    /// </summary>
    public static bool VerboseHandGridMpLog;

    /// <summary>
    /// <see cref="CardPile.Cards"/> order is not guaranteed to match across MP peers (layout / iteration). Sort by
    /// <see cref="NetCombatCardDb"/> ids so grid indexes refer to the same cards on host and observers.
    /// </summary>
    public static List<CardModel> StabilizeHandPileCandidates(IEnumerable<CardModel> cards) =>
        cards.OrderBy(c => NetCombatCardDb.Instance.GetCardId(c)).ToList();

    /// <summary>
    /// Hand cards for <see cref="FromSimpleGrid"/>: optional filter, optional exclusion (e.g. the card being played), then stabilize.
    /// Always pass the same rules to <c>rebuildCanonicalForRemoteApply</c> for MP.
    /// </summary>
    public static List<CardModel> BuildStabilizedHandCandidates(
        Player player,
        Func<CardModel, bool>? predicate,
        CardModel? excludeReference = null)
    {
        CardPile? hand = PileType.Hand.GetPile(player);
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

    /// <param name="rebuildCanonicalForRemoteApply">
    /// Non-local peers apply host indexes into the candidate list. Rebuild with the same rules as the choosing player
    /// immediately before applying indexes (field tribute: <see cref="TributeSummonSelection.BuildTributeSelectionCandidates"/>;
    /// hand: <see cref="BuildStabilizedHandCandidates"/>). Omitting this for hand grids causes MP state divergence.
    /// </param>
    public static async Task<IEnumerable<CardModel>> FromSimpleGrid(
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

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await context.SignalPlayerChoiceBegun(choiceBegunOptions);
        try
        {
            List<CardModel> result;
            if (ShouldSelectLocalCard(player))
            {
                if (CardSelectCmd.Selector != null)
                {
                    try
                    {
                        result = (await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect)).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        SyncCancelIfMp(player, choiceId);
                        throw;
                    }
                }
                else
                {
                    NPlayerHand.Instance?.CancelAllCardPlay();
                    NSimpleCardSelectScreen screen = NSimpleCardSelectScreen.Create(cards, prefs);
                    NOverlayStack.Instance.Push(screen);
                    try
                    {
                        result = (await screen.CardsSelected()).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        SyncCancelIfMp(player, choiceId);
                        throw;
                    }
                }

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
                    GD.Print($"[YgoDuelist][MP][Tribute] SyncLocalChoice choiceId={choiceId} ownerNet={player.NetId} indexes=[{idx}] candidates={preview}");
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
                    GD.Print($"[YgoDuelist][MP][Tribute] ApplyRemote choiceId={choiceId} ownerNet={player.NetId} indexes=[{idx}] resolve={preview}");
                }

                result = remoteIndexes.Select(i => resolve[i]).ToList();
            }

            return result;
        }
        finally
        {
            await context.SignalPlayerChoiceEnded();
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
            string extra = c is Mausoleum_Lose_HP m ? $" slot={m.MausoleumGridSlot}" : "";
            lines.Add($"  [{i}] {c.Id?.Entry}{extra}");
        }

        GD.PrintErr(string.Join("\n", lines));
    }

    private static void SyncCancelIfMp(Player player, uint choiceId)
    {
        if (!LocalContext.IsMe(player))
            return;
        if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            return;

        GD.Print($"[YgoDuelist][MP][Tribute] Grid cancel → SyncLocalChoice empty indexes choiceId={choiceId} ownerNet={player.NetId}");
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
            player,
            choiceId,
            PlayerChoiceResult.FromIndexes(new List<int>()));
    }
}
