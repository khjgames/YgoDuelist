using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Opens <see cref="NDeckCardSelectScreen"/> for trunk/side/split editing (deck-style multi-select + confirm); sort + nav UI is injected via Harmony.
/// Confirm applies moves without closing; Close or relic ends the overlay and the await.
/// </summary>
public static class TrunkSideDeckGuiService
{
    private static int _editorSessionActive;

    /// <summary>Local player for the in-progress <see cref="RunEditorAsync"/> session; set for the whole editor loop until exit.</summary>
    public static Player? EditorSessionPlayer { get; private set; }

    /// <summary>True while <see cref="RunEditorAsync"/> is in progress (including before the overlay is pushed). Used to ignore duplicate relic clicks.</summary>
    public static bool IsEditorSessionRunning() => Volatile.Read(ref _editorSessionActive) != 0;

    /// <summary>Set while <see cref="RunEditorAsync"/> is about to push <see cref="NDeckCardSelectScreen"/> so the trunk/side chrome patch can inject sort + nav controls.</summary>
    public static bool InjectNavButtonsOnNextGrid { get; private set; }

    /// <summary>
    /// While true, <see cref="NDeckCardSelectScreen"/> completes on the main Confirm (or on hitting max selection) without the preview overlay step.
    /// </summary>
    public static bool SkipDeckSelectPreviewLayer { get; private set; }

    /// <summary>Snapshot of trunk / side piles when opening split editor; used to build two columns. Cleared after the selection await.</summary>
    public static List<CardModel>? SplitSessionTrunkOrder { get; private set; }

    public static List<CardModel>? SplitSessionSideOrder { get; private set; }

    /// <summary>
    /// Split editor only: vertical <b>shift</b> of <c>YgoTrunkSideSplitHBox</c> after chrome without changing its height (patch applies the same value as <c>OffsetTop</c> += shift and <c>OffsetBottom</c> -= shift so split matches single-column vertical space). Use <see cref="SplitEditorHBoxOffsetBottomAdjust"/> only if you want to shorten/lengthen the split viewport.
    /// </summary>
    public static float SplitEditorHBoxOffsetTopAdjust { get; set; } = -80f;

    /// <summary>
    /// Split editor only: extra delta applied to split hbox <c>OffsetBottom</c> only (after the height-preserving shift). Use small positive values to pull the bottom edge up / shorten the grid area if clipping still feels too tall.
    /// </summary>
    public static float SplitEditorHBoxOffsetBottomAdjust { get; set; } = 0f;

    /// <summary>
    /// Split editor only: <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardGrid.YOffset"/> for both columns (internal card / scroll layout; vanilla deck screen uses 100).
    /// </summary>
    public static int SplitEditorNCardGridContentYOffset { get; set; } = 80;

    public static bool HasAnyTrunkOrSideCards(Player player)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return false;
        CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
        CardPile? side = YgoPlayerRunPiles.SideDeck(player);
        if (trunk == null || side == null)
            return false;
        return trunk.Cards.Count > 0 || side.Cards.Count > 0;
    }

    public static void SetInjectNavForNextGrid(bool value) => InjectNavButtonsOnNextGrid = value;

    public static void SetSkipDeckSelectPreviewLayer(bool value) => SkipDeckSelectPreviewLayer = value;

    public static void SetSplitSessionPiles(List<CardModel> trunk, List<CardModel> side)
    {
        SplitSessionTrunkOrder = trunk;
        SplitSessionSideOrder = side;
    }

    public static void ClearSplitSessionPiles()
    {
        SplitSessionTrunkOrder = null;
        SplitSessionSideOrder = null;
    }

    public static async Task RunEditorAsync(Player player)
    {
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;

        if (Interlocked.CompareExchange(ref _editorSessionActive, 1, 0) != 0)
            return;

        try
        {
            await RunEditorAsyncCore(player);
        }
        finally
        {
            Interlocked.Exchange(ref _editorSessionActive, 0);
        }
    }

    private static async Task RunEditorAsyncCore(Player player)
    {
        EditorSessionPlayer = player;
        try
        {
            EnsureActivePageShowsNonEmptyGrid(player);

            while (true)
            {
                if (!TryBuildCardList(player, TrunkSideDeckEditorSession.ActivePage, out List<CardModel> cards))
                {
                    return;
                }

                CardSelectorPrefs prefs = BuildPrefs(TrunkSideDeckEditorSession.ActivePage, cards.Count);
                SetInjectNavForNextGrid(true);
                SetSkipDeckSelectPreviewLayer(true);
                if (TrunkSideDeckEditorSession.ActivePage == TrunkSideDeckEditorPage.Split)
                {
                    CardPile? trunkPile = YgoPlayerRunPiles.Trunk(player);
                    CardPile? sidePile = YgoPlayerRunPiles.SideDeck(player);
                    if (trunkPile == null || sidePile == null)
                        return;
                    SetSplitSessionPiles(trunkPile.Cards.ToList(), sidePile.Cards.ToList());
                }

                YgoRelicBrowseGridOverlayPatch.SetPendingKind(YgoRelicBrowseGridOverlayPatch.RelicGridKind.TrunkSideDeckSelect);
                IEnumerable<CardModel> pickedEnumerable;
                try
                {
                    pickedEnumerable = await AwaitTrunkSideDeckGridSelection(player, cards, prefs);
                }
                catch (System.OperationCanceledException)
                {
                    TrunkSideDeckEditorSession.ClearNavigateRequest();
                    return;
                }
                finally
                {
                    SetInjectNavForNextGrid(false);
                    SetSkipDeckSelectPreviewLayer(false);
                    ClearSplitSessionPiles();
                    TrunkSideDeckSplitGridState.Clear();
                    YgoRelicBrowseGridOverlayPatch.ClearPendingKind();
                }

                if (TrunkSideDeckEditorSession.TryConsumeNavigateRequest(out TrunkSideDeckEditorPage next))
                {
                    TrunkSideDeckEditorSession.ActivePage = next;
                    EnsureActivePageShowsNonEmptyGrid(player);
                    continue;
                }

                List<CardModel> picked = pickedEnumerable.ToList();
                ApplyPicks(player, TrunkSideDeckEditorSession.ActivePage, picked);
                TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
                return;
            }
        }
        finally
        {
            EditorSessionPlayer = null;
        }
    }

    /// <summary>Applies trunk/side/split moves for the current editor page; notifies relic UI. Used when Confirm applies without closing the overlay.</summary>
    public static void ApplyTrunkSideEditorMoves(IReadOnlyList<CardModel> picked)
    {
        Player? p = EditorSessionPlayer;
        if (p == null || picked.Count == 0)
            return;
        ApplyPicks(p, TrunkSideDeckEditorSession.ActivePage, picked.ToList());
        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(p);
    }

    private static bool ShouldSelectLocalCard(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

    /// <summary>Same contract as <see cref="CardSelectCmd.FromSimpleGrid"/> (multiplayer sync + test selector), but uses <see cref="NDeckCardSelectScreen"/>.</summary>
    private static async Task<IEnumerable<CardModel>> AwaitTrunkSideDeckGridSelection(
        Player player,
        List<CardModel> cards,
        CardSelectorPrefs prefs)
    {
        if (CombatManager.Instance.IsEnding)
            return Array.Empty<CardModel>();

        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
            return cards.ToList();

        var context = YgoChoiceContexts.Blocking();
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
        List<CardModel> result;
        if (ShouldSelectLocalCard(player))
        {
            if (CardSelectCmd.Selector != null)
            {
                result = (await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect)).ToList();
            }
            else
            {
                NPlayerHand.Instance?.CancelAllCardPlay();
                NDeckCardSelectScreen screen = NDeckCardSelectScreen.Create(cards, prefs);
                NOverlayStack.Instance.Push(screen);
                result = (await screen.CardsSelected()).ToList();
            }

            List<int> indexes = result.Select(c => cards.IndexOf(c)).ToList();
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndexes(indexes));
        }
        else
        {
            result = (from i in (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndexes()
                select cards[i]).ToList();
        }

        await context.SignalPlayerChoiceEnded();
        return result;
    }

    private static void EnsureActivePageShowsNonEmptyGrid(Player player)
    {
        if (PageHasCards(player, TrunkSideDeckEditorSession.ActivePage))
            return;

        foreach (TrunkSideDeckEditorPage p in new[]
                 {
                     TrunkSideDeckEditorPage.Trunk,
                     TrunkSideDeckEditorPage.Side,
                     TrunkSideDeckEditorPage.Split
                 })
        {
            if (PageHasCards(player, p))
            {
                TrunkSideDeckEditorSession.ActivePage = p;
                return;
            }
        }
    }

    private static bool PageHasCards(Player player, TrunkSideDeckEditorPage page)
    {
        CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
        CardPile? side = YgoPlayerRunPiles.SideDeck(player);
        if (trunk == null || side == null)
            return false;
        return page switch
        {
            TrunkSideDeckEditorPage.Trunk => trunk.Cards.Count > 0,
            TrunkSideDeckEditorPage.Side => side.Cards.Count > 0,
            TrunkSideDeckEditorPage.Split => trunk.Cards.Count > 0 || side.Cards.Count > 0,
            _ => false
        };
    }

    private static bool TryBuildCardList(Player player, TrunkSideDeckEditorPage page, out List<CardModel> cards)
    {
        CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
        CardPile? side = YgoPlayerRunPiles.SideDeck(player);
        if (trunk == null || side == null)
        {
            cards = [];
            return false;
        }
        switch (page)
        {
            case TrunkSideDeckEditorPage.Trunk:
                cards = trunk.Cards.ToList();
                return true;
            case TrunkSideDeckEditorPage.Side:
                cards = side.Cards.ToList();
                return true;
            case TrunkSideDeckEditorPage.Split:
                cards = trunk.Cards.Concat(side.Cards).ToList();
                return true;
            default:
                cards = [];
                return false;
        }
    }

    private static CardSelectorPrefs BuildPrefs(TrunkSideDeckEditorPage page, int max)
    {
        string key = page switch
        {
            TrunkSideDeckEditorPage.Trunk => "YGODUELIST-TRUNK_SIDE_DECK_RELIC.prompt_trunk_edit",
            TrunkSideDeckEditorPage.Side => "YGODUELIST-TRUNK_SIDE_DECK_RELIC.prompt_side_edit",
            _ => "YGODUELIST-TRUNK_SIDE_DECK_RELIC.prompt_split_swap"
        };

        return new CardSelectorPrefs(new LocString("relics", key), 0, max)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
    }

    private static void ApplyPicks(Player player, TrunkSideDeckEditorPage page, List<CardModel> picked)
    {
        if (picked.Count == 0)
            return;

        CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
        CardPile? side = YgoPlayerRunPiles.SideDeck(player);
        if (trunk == null || side == null)
            return;

        switch (page)
        {
            case TrunkSideDeckEditorPage.Trunk:
                foreach (CardModel c in picked)
                {
                    if (!trunk.Cards.Contains(c))
                        continue;
                    trunk.RemoveInternal(c, silent: true);
                    side.AddInternal(c, -1, silent: true);
                }

                break;
            case TrunkSideDeckEditorPage.Side:
                foreach (CardModel c in picked)
                {
                    if (!side.Cards.Contains(c))
                        continue;
                    side.RemoveInternal(c, silent: true);
                    trunk.AddInternal(c, -1, silent: true);
                }

                break;
            case TrunkSideDeckEditorPage.Split:
                foreach (CardModel c in picked)
                {
                    if (trunk.Cards.Contains(c))
                    {
                        trunk.RemoveInternal(c, silent: true);
                        side.AddInternal(c, -1, silent: true);
                    }
                    else if (side.Cards.Contains(c))
                    {
                        side.RemoveInternal(c, silent: true);
                        trunk.AddInternal(c, -1, silent: true);
                    }
                }

                break;
        }
    }
}
