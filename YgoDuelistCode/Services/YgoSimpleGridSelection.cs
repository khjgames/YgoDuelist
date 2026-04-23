using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helper for non-combat or read-only simple-grid flows that still use the vanilla selection screen.
/// This keeps run/UI callers from repeating the same blocking context and cancel plumbing.
/// Do not use this for combat-path card picks that need synchronized combat-card wire semantics.
/// </summary>
public static class YgoSimpleGridSelection
{
    public static async Task<List<CardModel>> SelectAsync(
        Player player,
        IReadOnlyList<CardModel> cards,
        CardSelectorPrefs prefs)
    {
        return (await CardSelectCmd.FromSimpleGrid(
            YgoChoiceContexts.Blocking(),
            cards,
            player,
            prefs)).ToList();
    }

    public static async Task<List<CardModel>> TrySelectAsync(
        Player player,
        IReadOnlyList<CardModel> cards,
        CardSelectorPrefs prefs)
    {
        try
        {
            return await SelectAsync(player, cards, prefs);
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }
}
