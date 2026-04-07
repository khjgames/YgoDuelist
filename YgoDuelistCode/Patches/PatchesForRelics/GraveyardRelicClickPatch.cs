using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
[HarmonyPriority(Priority.First)]
public static class GraveyardRelicClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRelicInventory __instance, RelicModel model)
    {
        if (GraveyardRelic.IsGraveyardRelic(model))
            return !TryOpenGraveyardGrid(model);

        if (ShadowRealmRelic.IsShadowRealmRelic(model))
            return !TryOpenShadowRealmGrid(model);

        if (ExtraDeckRelic.IsExtraDeckRelic(model))
            return !TryOpenExtraDeckGrid(model);

        if (TrunkSideDeckRelic.IsTrunkSideDeckRelic(model))
            return !TryTrunkSideDeckRelicClick(model);

        return true;
    }

    /// <summary>
    /// <see cref="RunManager.DebugOnlyGetState"/> is null during some combat moments; <see cref="LocalContext.GetMe(CombatState)"/> still resolves the local player.
    /// </summary>
    private static Player? ResolveLocalPlayerForZoneRelicClick()
    {
        IRunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState != null)
            return LocalContext.GetMe((IPlayerCollection)runState);

        if (CombatManager.Instance is { IsInProgress: true })
        {
            CombatState? combat = CombatManager.Instance.DebugOnlyGetState();
            if (combat != null)
                return LocalContext.GetMe(combat);
        }

        return null;
    }

    private static bool TryOpenGraveyardGrid(RelicModel model)
    {
        GraveyardRelic? graveyard = GraveyardRelic.AsGraveyard(model);
        if (graveyard == null)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.TryToggleClose(YgoRelicBrowseGridOverlayPatch.RelicGridKind.Graveyard))
            return true;

        Player? player = ResolveLocalPlayerForZoneRelicClick();
        if (player == null)
            return false;

        IReadOnlyList<CardModel> cards = GraveyardRelic.GetGraveyardCards(player);
        if (cards.Count == 0)
            return false;

        return TryOpenRelicCardGrid(YgoRelicBrowseGridOverlayPatch.RelicGridKind.Graveyard, model, player, cards);
    }

    private static bool TryOpenShadowRealmGrid(RelicModel model)
    {
        ShadowRealmRelic? shadow = ShadowRealmRelic.AsShadowRealm(model);
        if (shadow == null)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.TryToggleClose(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ShadowRealm))
            return true;

        Player? player = ResolveLocalPlayerForZoneRelicClick();
        if (player == null)
            return false;

        IReadOnlyList<CardModel> cards = ShadowRealmRelic.GetShadowRealmCards(player);
        if (cards.Count == 0)
            return false;

        return TryOpenRelicCardGrid(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ShadowRealm, model, player, cards);
    }

    private static bool TryOpenExtraDeckGrid(RelicModel model)
    {
        ExtraDeckRelic? extra = ExtraDeckRelic.AsExtraDeck(model);
        if (extra == null)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.TryToggleClose(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ExtraDeck))
            return true;

        Player? player = ResolveLocalPlayerForZoneRelicClick();
        if (player == null)
            return false;

        IReadOnlyList<CardModel> cards = ExtraDeckRelic.GetExtraDeckCards(player);
        if (cards.Count == 0)
            return false;

        return TryOpenRelicCardGrid(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ExtraDeck, model, player, cards);
    }

    private static bool TryTrunkSideDeckRelicClick(RelicModel model)
    {
        if (TrunkSideDeckRelic.AsTrunkSideDeck(model) == null)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.TryToggleClose(YgoRelicBrowseGridOverlayPatch.RelicGridKind.TrunkSideDeckSelect))
            return true;

        Player? player = ResolveLocalPlayerForZoneRelicClick();
        if (player == null || !TrunkSideDeckGuiService.HasAnyTrunkOrSideCards(player))
            return false;

        // Avoid stacking editors: pending kind is set only right before the grid opens, so duplicate clicks
        // in that window used to start a second RunEditorAsync. Toggle is handled above once the overlay exists.
        if (TrunkSideDeckGuiService.IsEditorSessionRunning())
            return true;

        YgoRelicBrowseGridOverlayPatch.CloseAnyActiveBrowseGrid();
        TaskHelper.RunSafely(TrunkSideDeckGuiService.RunEditorAsync(player));
        return true;
    }

    private static bool TryOpenRelicCardGrid(
        YgoRelicBrowseGridOverlayPatch.RelicGridKind gridKind,
        RelicModel model,
        Player player,
        IReadOnlyList<CardModel> cards)
    {
        if (YgoRelicBrowseGridOverlayPatch.IsRelicSimpleGridOpenInFlight(gridKind))
            return true;

        YgoRelicBrowseGridOverlayPatch.CloseAnyActiveBrowseGrid();

        var selectionPromptProp = AccessTools.Property(typeof(RelicModel), "SelectionScreenPrompt");
        object? selectionPrompt = selectionPromptProp.GetValue(model);

        // Cancelable: SimpleCardSelectScreenCancelBackButtonPatch / NSimpleCardSelectScreenCancelablePatch wire close/back to TrySetCanceled + Remove (same close path we need for toggle).
        var prefs = new CardSelectorPrefs(
            (dynamic)selectionPrompt!,
            0,
            0)
        {
            Cancelable = true
        };

        // Set before the async work starts so overlay tracking matches the first Push; duplicate Pushes dismiss the prior grid in AfterOverlayPush.
        YgoRelicBrowseGridOverlayPatch.SetPendingKind(gridKind);
        TaskHelper.RunSafely(ShowAsync());

        async Task ShowAsync()
        {
            try
            {
                await CardSelectCmd.FromSimpleGrid(
                    new BlockingPlayerChoiceContext(),
                    cards,
                    player,
                    prefs);
            }
            catch (OperationCanceledException)
            {
                // Cancelable=true: back/close (SimpleCardSelectScreenCancelBackButtonPatch) uses TrySetCanceled on the grid task.
            }
            finally
            {
                YgoRelicBrowseGridOverlayPatch.ClearPendingKind();
            }
        }

        return true;
    }
}
