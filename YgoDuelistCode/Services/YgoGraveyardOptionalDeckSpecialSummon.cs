using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Optional activate in GY, then Special Summon from deck — <see cref="IGraveyardOptionalDeckSpecialSummon"/> (no mark) and
/// <see cref="IBattleDeathOptionalDeckSpecialSummon"/> (requires <see cref="YgoBattleDeathMarkedCards"/> consume).
/// </summary>
public static class YgoGraveyardOptionalDeckSpecialSummon
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not BaseMonsterCard source)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        if (source is IGraveyardOptionalDeckSpecialSummon gy)
        {
            TaskHelper.RunSafely(RunAsync(player, source, gy.GraveyardActivatePrompt, gy.GraveyardSummonPrompt, gy.IsGraveyardDeckSummonCandidate));
            return;
        }

        if (source is not IBattleDeathOptionalDeckSpecialSummon bd)
            return;
        if (!YgoBattleDeathMarkedCards.Consume(source))
            return;

        TaskHelper.RunSafely(RunAsync(player, source, bd.BattleDeathActivatePrompt, bd.BattleDeathSummonPrompt, bd.IsBattleDeathDeckSummonCandidate));
    }

    private static List<CardModel> BuildDeckCandidates(Player player, Func<BaseMonsterCard, bool> isCandidate)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards)
            .OfType<BaseMonsterCard>()
            .Where(isCandidate)
            .Cast<CardModel>()
            .ToList();
    }

    private static async Task RunAsync(
        Player player,
        BaseMonsterCard source,
        LocString activatePrompt,
        LocString summonPrompt,
        Func<BaseMonsterCard, bool> isCandidate)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            source,
            activatePrompt);
        if (ctx == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        var summonPrefs = new CardSelectorPrefs(summonPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<BaseMonsterCard> BuildTypedDeckCandidates() =>
            BuildDeckCandidates(player, isCandidate).OfType<BaseMonsterCard>().ToList();

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            summonPrefs,
            BuildTypedDeckCandidates);
        if (chosen == null)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }
}
