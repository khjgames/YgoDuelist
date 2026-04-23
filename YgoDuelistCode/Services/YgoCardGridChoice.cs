using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared synced-grid helpers for the common "choose one card from a combat card list" pattern.
/// Keeps card effects thin while routing selection through the MP-safe combat-wire path.
/// Prefer <see cref="YgoOrderedCardSelection"/> when the caller is only rebuilding an ordered list and interpreting the result.
/// </summary>
public static class YgoCardGridChoice
{
    /// <summary>
    /// Caller owns candidate construction and any canonical ordering requirements.
    /// This helper owns the synced combat-grid wire and stable typed result extraction.
    /// </summary>
    public static async Task<TCard?> TryChooseSingleAsync<TCard>(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> candidates,
        Player player,
        CardSelectorPrefs prefs,
        Func<List<CardModel>>? rebuildCanonicalForRemoteApply = null)
        where TCard : CardModel
    {
        if (candidates.Count == 0)
            return null;

        IEnumerable<CardModel> picked;
        try
        {
            picked = await TributeSummonGridSelect.FromSimpleGridCombat(
                context,
                candidates,
                player,
                prefs,
                rebuildCanonicalForRemoteApply: rebuildCanonicalForRemoteApply);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        return YgoMpCombatOrder.FirstCardWhereStable(picked, c => c is TCard) as TCard;
    }
}
