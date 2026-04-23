using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helper for activated effects that choose a fusion monster directly from the extra deck.
/// Cards still own the payoff; this helper owns candidate ordering and synced selection plumbing.
/// </summary>
public static class YgoFusionExtraDeckSelection
{
    public static List<FusionMonsterCard> BuildFusionCardsInExtraDeck(Player player) =>
        BuildFusionCandidates(player);

    public static List<FusionMonsterCard> BuildFusionCandidates(Player player)
    {
        CardPile? extra = YgoPlayerPiles.ExtraDeck(player);
        return extra == null
            ? []
            : YgoMpCombatOrder.CardsSnapshotOrderedForMp(extra.Cards)
                .OfType<FusionMonsterCard>()
                .ToList();
    }

    public static async Task<FusionMonsterCard?> TryChooseFusionFromExtraDeckAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CardSelectorPrefs prefs)
    {
        List<FusionMonsterCard> candidates = BuildFusionCandidates(player);
        if (candidates.Count == 0)
            return null;
        if (candidates.Count == 1)
            return candidates[0];

        return await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            prefs,
            () => BuildFusionCandidates(player));
    }
}
