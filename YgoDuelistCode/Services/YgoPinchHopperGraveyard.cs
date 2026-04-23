using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Pinch_Hopper"/>: when sent to the Graveyard, optional activation (shows this card), then Special Summon 1 Insect monster from the hand.
/// </summary>
public static class YgoPinchHopperGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-PINCH_HOPPER.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-PINCH_HOPPER.summon_insect");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Pinch_Hopper pinch)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        TaskHelper.RunSafely(RunAsync(player, pinch));
    }

    private static List<CardModel> BuildHandCandidates(Player player)
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Insect && m.CanSummonDuelMonster)
            .Cast<CardModel>()
            .ToList();
    }

    private static async Task RunAsync(Player player, Pinch_Hopper pinch)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        if (pinch.Pile?.Type != GraveyardPile.CustomType)
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            pinch,
            ActivatePrompt);
        if (ctx == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<BaseMonsterCard> BuildTypedHandCandidates() =>
            BuildHandCandidates(player).OfType<BaseMonsterCard>().ToList();

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            summonPrefs,
            BuildTypedHandCandidates);
        if (chosen == null)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }
}
