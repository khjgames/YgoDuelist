using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helper for choose-one creature flows that present <see cref="YgoEnemyIntentProxyCard"/> rows.
/// </summary>
public static class YgoCreatureProxySelection
{
    public static async Task<Creature?> TryChooseSingleCreatureAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<Creature> candidates,
        LocString prompt,
        bool cancelable)
    {
        if (candidates.Count == 0)
            return null;
        if (candidates.Count == 1)
            return candidates[0];

        List<YgoEnemyIntentProxyCard> BuildCurrentProxyCards() => BuildProxyCards(candidates);

        YgoEnemyIntentProxyCard? proxy = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(prompt, 1, 1) { Cancelable = cancelable },
            BuildCurrentProxyCards);
        Creature? chosen = proxy?.TargetCreature;
        return chosen != null && chosen.IsAlive && candidates.Contains(chosen) ? chosen : null;
    }

    private static List<YgoEnemyIntentProxyCard> BuildProxyCards(IReadOnlyList<Creature> candidates) => candidates
        .Select(c => new YgoEnemyIntentProxyCard(c))
        .ToList();
}
