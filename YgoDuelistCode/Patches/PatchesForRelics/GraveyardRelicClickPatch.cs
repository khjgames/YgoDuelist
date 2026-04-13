using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
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
            return !TryZoneRelicPageClick(ZoneRelicViewPage.Graveyard, GraveyardRelic.GetGraveyardCards);

        if (BanishedRelic.IsBanishedRelic(model))
            return !TryZoneRelicPageClick(ZoneRelicViewPage.Banished, BanishedRelic.GetBanishedCards);

        if (ExtraDeckRelic.IsExtraDeckRelic(model))
            return !TryZoneRelicPageClick(ZoneRelicViewPage.ExtraDeck, ExtraDeckRelic.GetExtraDeckCards);

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

    private static bool TryZoneRelicPageClick(ZoneRelicViewPage page, Func<Player, IReadOnlyList<CardModel>> getCards)
    {
        if (ZoneRelicViewGuiService.IsViewerSessionRunning())
        {
            if (YgoRelicBrowseGridOverlayPatch.TryToggleCloseZoneView(page))
                return true;
            if (YgoRelicBrowseGridOverlayPatch.IsZoneViewGridOpenInFlight())
                return true;
            YgoRelicBrowseGridOverlayPatch.CompleteActiveZoneViewNavigate(page);
            return true;
        }

        Player? player = ResolveLocalPlayerForZoneRelicClick();
        if (player == null)
            return false;

        if (getCards(player).Count == 0)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.IsZoneViewGridOpenInFlight())
            return true;

        YgoRelicBrowseGridOverlayPatch.CloseAnyActiveBrowseGrid();
        ZoneRelicViewSession.ActivePage = page;
        TaskHelper.RunSafely(ZoneRelicViewGuiService.RunViewerAsync(player));
        return true;
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
}
