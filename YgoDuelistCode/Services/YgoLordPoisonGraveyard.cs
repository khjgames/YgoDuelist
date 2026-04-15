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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
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

    private static List<BaseMonsterCard> CollectPlantsExceptLordPoison(Player player)
    {
        CardPile? gy = GraveyardRelic.GetGraveyardPile(player);
        if (gy == null)
            return [];

        return gy.Cards
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Plant && m is not Lord_Poison && m.CanSummonDuelMonster)
            .ToList();
    }

    private static async Task RunAsync(Player player, Lord_Poison sourceInGraveyard)
    {
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> plants = CollectPlantsExceptLordPoison(player);
        if (plants.Count == 0)
            return;

        var ctx = new BlockingPlayerChoiceContext();

        var activatePrefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> activationPick = await CardSelectCmd.FromSimpleGrid(
            ctx,
            new[] { sourceInGraveyard },
            player,
            activatePrefs);

        if (activationPick.FirstOrDefault() is not Lord_Poison)
            return;

        plants = CollectPlantsExceptLordPoison(player);
        if (plants.Count == 0)
            return;

        var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> summonPick = await CardSelectCmd.FromSimpleGrid(ctx, plants, player, summonPrefs);
        if (summonPick.FirstOrDefault() is not BaseMonsterCard chosen)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }
}
