using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// MP-safe helper for preview-only grids that require manual confirmation but do not return a meaningful selection.
/// These prompts still consume a synced player-choice id, so they should not call vanilla <c>CardSelectCmd.FromSimpleGrid</c>
/// directly on combat paths.
/// </summary>
public static class YgoPreviewGridSelection
{
    public static Task ShowPreviewAsync(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards,
        Player player,
        LocString prompt,
        PlayerChoiceOptions choiceBegunOptions = PlayerChoiceOptions.None) =>
        ShowPreviewAsync(
            context,
            cards,
            player,
            new CardSelectorPrefs(prompt, 0, 0)
            {
                RequireManualConfirmation = true,
                Cancelable = false
            },
            choiceBegunOptions);

    public static async Task ShowPreviewAsync(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards,
        Player player,
        CardSelectorPrefs prefs,
        PlayerChoiceOptions choiceBegunOptions = PlayerChoiceOptions.None)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(prefs);

        await TributeSummonGridSelect.FromSimpleGridIndexed(
            context,
            cards,
            player,
            prefs,
            rebuildCanonicalForRemoteApply: () => new List<CardModel>(cards),
            choiceBegunOptions: choiceBegunOptions);
    }
}
