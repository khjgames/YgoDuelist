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

/// <summary>
/// When the CardOptions relic is clicked, open a read-only grid view of the
/// current YgoCardOptionPile contents.
/// </summary>
[HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
public static class CardOptionsRelicClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRelicInventory __instance, RelicModel model)
    {
        if (!CardOptionsRelic.IsCardOptionsRelic(model))
            return true;

        var cardOptions = CardOptionsRelic.AsCardOptions(model);
        if (cardOptions == null)
            return true;

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
            return true;

        var player = LocalContext.GetMe((IPlayerCollection)runState);
        if (player == null)
            return true;

        var cards = CardOptionsRelic.GetOptionCards(player);
        if (cards.Count == 0)
            return false; // handled (do nothing)

        var prompt = new LocString("relics", "CARD_OPTIONS.selectionScreenPrompt");
        var prefs = new CardSelectorPrefs(prompt, 0, 0);

        TaskHelper.RunSafely(ShowOptionsAsync());

        async Task ShowOptionsAsync()
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
