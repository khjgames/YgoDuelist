using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared confirmation flow for graveyard-triggered effects that activate from one concrete source card.
/// </summary>
public static class YgoGraveyardTriggeredActivation
{
    public static async Task<PlayerChoiceContext?> TryConfirmSourceAsync<TCard>(
        Player player,
        TCard source,
        LocString activatePrompt)
        where TCard : BaseMonsterCard
    {
        var ctx = YgoChoiceContexts.Blocking();
        var activatePrefs = new CardSelectorPrefs(activatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        TCard? activationPick = await YgoOrderedCardSelection.TryConfirmSingleCardAsync(
            ctx,
            player,
            activatePrefs,
            source);
        return ReferenceEquals(activationPick, source) ? ctx : null;
    }
}
