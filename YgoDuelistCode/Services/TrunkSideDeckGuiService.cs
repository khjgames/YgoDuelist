using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Opens <see cref="CardSelectCmd.FromSimpleGrid"/> for trunk/side/split editing; nav buttons are injected on the grid via Harmony.
/// </summary>
public static class TrunkSideDeckGuiService
{
    private static int _editorSessionActive;

    /// <summary>True while <see cref="RunEditorAsync"/> is in progress (including before the overlay is pushed). Used to ignore duplicate relic clicks.</summary>
    public static bool IsEditorSessionRunning() => Volatile.Read(ref _editorSessionActive) != 0;

    /// <summary>Set while <see cref="RunEditorAsync"/> is about to push the simple select screen so the trunk/side nav bar patch can inject buttons (after overlay open).</summary>
    public static bool InjectNavButtonsOnNextGrid { get; private set; }

    public static bool HasAnyTrunkOrSideCards(Player player)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return false;
        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        return trunk.Cards.Count > 0 || side.Cards.Count > 0;
    }

    public static void SetInjectNavForNextGrid(bool value) => InjectNavButtonsOnNextGrid = value;

    public static async Task RunEditorAsync(Player player)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
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
        EnsureActivePageShowsNonEmptyGrid(player);

        while (true)
        {
            if (!TryBuildCardList(player, TrunkSideDeckEditorSession.ActivePage, out List<CardModel> cards)
                || cards.Count == 0)
            {
                return;
            }

            CardSelectorPrefs prefs = BuildPrefs(TrunkSideDeckEditorSession.ActivePage, cards.Count);
            SetInjectNavForNextGrid(true);
            YgoRelicBrowseGridOverlayPatch.SetPendingKind(YgoRelicBrowseGridOverlayPatch.RelicGridKind.TrunkSideDeckSelect);
            IEnumerable<CardModel> pickedEnumerable;
            try
            {
                pickedEnumerable = await CardSelectCmd.FromSimpleGrid(
                    new BlockingPlayerChoiceContext(),
                    cards,
                    player,
                    prefs);
            }
            catch (System.OperationCanceledException)
            {
                TrunkSideDeckEditorSession.ClearNavigateRequest();
                return;
            }
            finally
            {
                SetInjectNavForNextGrid(false);
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
        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
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
        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);
        switch (page)
        {
            case TrunkSideDeckEditorPage.Trunk:
                cards = trunk.Cards.ToList();
                return cards.Count > 0;
            case TrunkSideDeckEditorPage.Side:
                cards = side.Cards.ToList();
                return cards.Count > 0;
            case TrunkSideDeckEditorPage.Split:
                cards = trunk.Cards.Concat(side.Cards).ToList();
                return cards.Count > 0;
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

        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(player);

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
