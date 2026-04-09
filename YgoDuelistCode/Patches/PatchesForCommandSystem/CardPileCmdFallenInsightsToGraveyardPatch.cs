using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="FallenInsightsPower"/>: count your cards moved to the Graveyard; draw on threshold.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdFallenInsightsToGraveyardPatch
{
    [HarmonyPrefix]
    public static void Prefix(IEnumerable<CardModel> cards, CardPile newPile, ref List<(CardModel Card, PileType? From)>? __state)
    {
        __state = new List<(CardModel, PileType?)>();
        foreach (CardModel c in cards)
            __state.Add((c, c.Pile?.Type));
    }

    [HarmonyPostfix]
    public static void Postfix(Task __result, CardPile newPile, List<(CardModel Card, PileType? From)>? __state)
    {
        if (__state == null || newPile.Type != GraveyardPile.CustomType)
            return;

        _ = AfterGraveyardAddAsync(__result, __state);
    }

    private static async Task AfterGraveyardAddAsync(Task moveCompleted, List<(CardModel Card, PileType? From)> state)
    {
        await moveCompleted;

        var ctx = new BlockingPlayerChoiceContext();

        foreach ((CardModel card, _) in state)
        {
            Player? owner = card.Owner;
            if (owner?.Creature == null)
                continue;
            if (owner.Creature.GetPower<FallenInsightsPower>() is not { } insight)
                continue;

            await insight.OnOwnerCardsAddedToGraveyardAsync(ctx, 1);
        }
    }
}
