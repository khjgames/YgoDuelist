using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
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

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
public static class GraveyardRelicClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRelicInventory __instance, RelicModel model)
    {
        if (GraveyardRelic.IsGraveyardRelic(model))
            return !TryOpenGraveyardGrid(model);

        if (ExtraDeckRelic.IsExtraDeckRelic(model))
            return !TryOpenExtraDeckGrid(model);

        return true;
    }

    private static bool TryOpenGraveyardGrid(RelicModel model)
    {
        GraveyardRelic? graveyard = GraveyardRelic.AsGraveyard(model);
        if (graveyard == null)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.TryToggleClose(YgoRelicBrowseGridOverlayPatch.RelicGridKind.Graveyard))
            return true;

        IRunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
            return false;

        Player? player = LocalContext.GetMe((IPlayerCollection)runState);
        if (player == null)
            return false;

        IReadOnlyList<CardModel> cards = GraveyardRelic.GetGraveyardCards(player);
        if (cards.Count == 0)
            return false;

        return TryOpenRelicCardGrid(YgoRelicBrowseGridOverlayPatch.RelicGridKind.Graveyard, model, player, cards);
    }

    private static bool TryOpenExtraDeckGrid(RelicModel model)
    {
        ExtraDeckRelic? extra = ExtraDeckRelic.AsExtraDeck(model);
        if (extra == null)
            return false;

        if (YgoRelicBrowseGridOverlayPatch.TryToggleClose(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ExtraDeck))
            return true;

        IRunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
            return false;

        Player? player = LocalContext.GetMe((IPlayerCollection)runState);
        if (player == null)
            return false;

        IReadOnlyList<CardModel> cards = ExtraDeckRelic.GetExtraDeckCards(player);
        if (cards.Count == 0)
            return false;

        return TryOpenRelicCardGrid(YgoRelicBrowseGridOverlayPatch.RelicGridKind.ExtraDeck, model, player, cards);
    }

    private static bool TryOpenRelicCardGrid(
        YgoRelicBrowseGridOverlayPatch.RelicGridKind gridKind,
        RelicModel model,
        Player player,
        IReadOnlyList<CardModel> cards)
    {
        YgoRelicBrowseGridOverlayPatch.CloseAnyActiveBrowseGrid();

        var selectionPromptProp = AccessTools.Property(typeof(RelicModel), "SelectionScreenPrompt");
        object? selectionPrompt = selectionPromptProp.GetValue(model);

        var prefs = new CardSelectorPrefs(
            (dynamic)selectionPrompt!,
            0,
            0);

        TaskHelper.RunSafely(ShowAsync());

        async Task ShowAsync()
        {
            YgoRelicBrowseGridOverlayPatch.SetPendingKind(gridKind);
            try
            {
                await CardSelectCmd.FromSimpleGrid(
                    new BlockingPlayerChoiceContext(),
                    cards,
                    player,
                    prefs);
            }
            finally
            {
                YgoRelicBrowseGridOverlayPatch.ClearPendingKind();
            }
        }

        return true;
    }
}
