using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
        if (!GraveyardRelic.IsGraveyardRelic(model))
            return true;

        var graveyard = GraveyardRelic.AsGraveyard(model);
        if (graveyard == null)
            return true;

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
            return true;

        var player = LocalContext.GetMe((IPlayerCollection)runState);
        if (player == null)
            return true;

        var cards = GraveyardRelic.GetGraveyardCards(player);
        if (cards.Count == 0)
            return true;

        // Use the relic's own (non-public) SelectionScreenPrompt so localization
        // still comes from the game JSON instead of a hardcoded string.
        var selectionPromptProp = AccessTools.Property(typeof(RelicModel), "SelectionScreenPrompt");
        var selectionPrompt = selectionPromptProp.GetValue(model);

        var prefs = new CardSelectorPrefs(
            (dynamic)selectionPrompt,
            0,
            0);

        TaskHelper.RunSafely(ShowGraveyardCardsAsync());

        async Task ShowGraveyardCardsAsync()
        {
            await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                cards,
                player,
                prefs);
        }

        return false;
    }
}
