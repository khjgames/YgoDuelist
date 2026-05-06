using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Lord_Poison"/>: when destroyed by battle and sent to the Graveyard, optionally Special Summon 1 Plant from your Graveyard except "Lord Poison".
/// </summary>
public static class YgoLordPoisonGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-LORD_POISON.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-LORD_POISON.summon_plant");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Lord_Poison lp)
            return;
        if (!YgoBattleDeathMarkedCards.Consume(lp))
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? player))
            return;

        TaskHelper.RunSafely(RunAsync(player, lp));
    }

    private static List<CardModel> BuildGraveyardCandidates(Player player)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m =>
                m.DuelMonsterRace == DuelMonsterRace.Plant
                && m is not Lord_Poison
                && m.CanSummonDuelMonster
                && ReactorSlimeSummonGate.AllowsSummon(player, m))
            .Cast<CardModel>()
            .ToList();
    }

    private static async Task RunAsync(Player player, Lord_Poison sourceInGraveyard)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            sourceInGraveyard,
            ActivatePrompt);
        if (ctx == null)
            return;

        var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<BaseMonsterCard> BuildTypedGraveyardCandidates() =>
            BuildGraveyardCandidates(player).OfType<BaseMonsterCard>().ToList();

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            summonPrefs,
            BuildTypedGraveyardCandidates);
        if (chosen == null)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }
}
