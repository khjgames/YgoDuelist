using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Single browse session for Graveyard / Shadow Realm / Extra Deck relic grids; navigation matches <see cref="TrunkSideDeckGuiService"/> (loop + dismiss + consume navigate).
/// </summary>
public static class ZoneRelicViewGuiService
{
    private static int _viewerSessionActive;

    /// <summary>Local player for the in-progress <see cref="RunViewerAsync"/> loop.</summary>
    public static Player? ViewerSessionPlayer { get; private set; }

    public static bool IsViewerSessionRunning() => Volatile.Read(ref _viewerSessionActive) != 0;

    public static async Task RunViewerAsync(Player player)
    {
        if (Interlocked.CompareExchange(ref _viewerSessionActive, 1, 0) != 0)
            return;

        try
        {
            await RunViewerAsyncCore(player);
        }
        finally
        {
            Interlocked.Exchange(ref _viewerSessionActive, 0);
        }
    }

    private static async Task RunViewerAsyncCore(Player player)
    {
        ViewerSessionPlayer = player;
        try
        {
            while (true)
            {
                IReadOnlyList<CardModel> cards = GetCardsForActivePage(player);
                RelicModel? relic = GetRelicModelForPage(player, ZoneRelicViewSession.ActivePage);
                if (relic == null)
                    return;

                var selectionPromptProp = AccessTools.Property(typeof(RelicModel), "SelectionScreenPrompt");
                object? selectionPrompt = selectionPromptProp.GetValue(relic);
                var prefs = new CardSelectorPrefs(
                    (dynamic)selectionPrompt!,
                    0,
                    0)
                {
                    Cancelable = true
                };

                YgoRelicBrowseGridOverlayPatch.SetPendingKind(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ZoneRelicView);
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
                    ZoneRelicViewSession.ClearNavigateRequest();
                    return;
                }
                finally
                {
                    YgoRelicBrowseGridOverlayPatch.ClearPendingKind();
                }

                if (ZoneRelicViewSession.TryConsumeNavigateRequest(out ZoneRelicViewPage next))
                {
                    ZoneRelicViewSession.ActivePage = next;
                    continue;
                }

                return;
            }
        }
        finally
        {
            ViewerSessionPlayer = null;
        }
    }

    private static IReadOnlyList<CardModel> GetCardsForActivePage(Player player) =>
        ZoneRelicViewSession.ActivePage switch
        {
            ZoneRelicViewPage.Graveyard => GraveyardRelic.GetGraveyardCards(player),
            ZoneRelicViewPage.ShadowRealm => ShadowRealmRelic.GetShadowRealmCards(player),
            ZoneRelicViewPage.ExtraDeck => ExtraDeckRelic.GetExtraDeckCards(player),
            _ => Array.Empty<CardModel>()
        };

    private static RelicModel? GetRelicModelForPage(Player player, ZoneRelicViewPage page) =>
        page switch
        {
            ZoneRelicViewPage.Graveyard => player.Relics.OfType<GraveyardRelic>().FirstOrDefault(),
            ZoneRelicViewPage.ShadowRealm => player.Relics.OfType<ShadowRealmRelic>().FirstOrDefault(),
            ZoneRelicViewPage.ExtraDeck => player.Relics.OfType<ExtraDeckRelic>().FirstOrDefault(),
            _ => null
        };
}
