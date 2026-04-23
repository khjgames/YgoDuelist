using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Before Neow blessing options, YgoDuelist picks 9–<see cref="MaxPick"/> cards from a structured grid of <see cref="YgoCardPackTags.Starter"/> cards (<see cref="YgoStarterCardCatalog.GridSize"/> slots, or <see cref="YgoStarterCardCatalog.MaxGridSize"/> when a ritual spell gains a bundled monster row).
/// Picks go to the deck; the rest go to the <see cref="PlayerRunTrunk"/> (Deck_Trunk_Side_System).
/// </summary>
public static class YgoNeowStarterDeckGridService
{
    public const int GridSize = YgoStarterCardCatalog.GridSize;
    public const int MinPick = 8;
    public const int MaxPick = YgoStarterCardCatalog.MaxGridSize;

    /// <summary>Neow instance for which the next <see cref="AncientEventModel.SetInitialEventState"/> call must not run the starter draft again (async-safe vs ThreadStatic).</summary>
    private static AncientEventModel? sResumeNeowWithoutStarterDraft;

    private static readonly MethodInfo SetInitialEventState = AccessTools.DeclaredMethod(typeof(AncientEventModel), "SetInitialEventState")
        ?? throw new InvalidOperationException("AncientEventModel.SetInitialEventState not found");

    public static bool ShouldSkipStarterDraftInterceptAndClear(AncientEventModel instance)
    {
        if (!ReferenceEquals(sResumeNeowWithoutStarterDraft, instance))
            return false;
        sResumeNeowWithoutStarterDraft = null;
        return true;
    }

    private static void DraftLog(string msg)
    {
        int tid = System.Environment.CurrentManagedThreadId;
        bool main = NGame.IsMainThread();
        GD.Print($"[YgoDuelist NeowDraft] (tid={tid} mainThread={main}) {msg}");
        MainFile.Logger.Info($"[NeowDraft async tid={tid} main={main}] {msg}");
    }

    public static async Task RunDraftThenResumeNeowAsync(AncientEventModel neowEvent, bool isPreFinished)
    {
        DraftLog("RunDraftThenResumeNeowAsync START");
        try
        {
            Player player = neowEvent.Owner
                ?? throw new InvalidOperationException("Neow event has no owner during starter grid.");

            DraftLog($"building grid size={GridSize}");
            List<CardModel> grid = YgoStarterCardCatalog.CreateRandomGrid(neowEvent.Rng, GridSize);
            DraftLog($"grid built count={grid.Count} distinctIds={grid.Select(c => c.Id.Entry).Distinct().Count()}");

            int maxSelect = grid.Count;
            if (maxSelect > MaxPick)
                throw new InvalidOperationException($"Neow starter grid count {maxSelect} exceeds {nameof(MaxPick)} {MaxPick}.");

            var prefs = new CardSelectorPrefs(
                new LocString("combat_messages", "YGODUELIST-NEOW_STARTER_GRID.prompt"),
                MinPick,
                maxSelect)
            {
                Cancelable = false
            };

            DraftLog("await YgoSimpleGridSelection.SelectAsync…");
            List<CardModel> chosenList = await YgoSimpleGridSelection.SelectAsync(player, grid, prefs);
            DraftLog($"FromSimpleGrid returned chosenCount={chosenList.Count}");

            // Deck.AddInternal alone does not set Owner or RunState._allCards; Hook.ShouldAllowAncient → RunState.Contains NREs on Owner.
            var chosenRefs = new HashSet<CardModel>(chosenList);
            foreach (CardModel c in chosenList)
            {
                player.RunState.AddCard(c, player);
                c.FloorAddedToDeck = 1;
                player.Deck.AddInternal(c, -1, silent: false);
                c.AfterCreated();
            }

            CardPile? trunk = YgoPlayerRunPiles.Trunk(player);
            if (trunk == null)
                return;
            int trunkCount = 0;
            foreach (CardModel c in grid)
            {
                if (chosenRefs.Contains(c))
                    continue;
                player.RunState.AddCard(c, player);
                c.FloorAddedToDeck = 1;
                trunk.AddInternal(c, -1, silent: true);
                c.AfterCreated();
                trunkCount++;
            }

            TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
            // NTopBarDeckButton only listens to CardAddFinished; AddInternal never raises it (game uses it after VFX/commands).
            player.Deck.InvokeCardAddFinished();
            DraftLog($"deck +{chosenList.Count} (deckCount={player.Deck.Cards.Count}); trunk +{trunkCount} unchosen; invoked Deck.InvokeCardAddFinished for UI");
            DraftLog("invoking SetInitialEventState (resume) via reflection…");
            sResumeNeowWithoutStarterDraft = neowEvent;
            try
            {
                SetInitialEventState.Invoke(neowEvent, new object[] { isPreFinished });
            }
            finally
            {
                if (ReferenceEquals(sResumeNeowWithoutStarterDraft, neowEvent))
                    sResumeNeowWithoutStarterDraft = null;
            }

            DraftLog("SetInitialEventState resume finished OK");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YgoDuelist NeowDraft] FATAL: {ex}");
            MainFile.Logger.Error($"[NeowDraft] FATAL: {ex}");
            throw;
        }
        finally
        {
            DraftLog("RunDraftThenResumeNeowAsync END");
        }
    }
}
