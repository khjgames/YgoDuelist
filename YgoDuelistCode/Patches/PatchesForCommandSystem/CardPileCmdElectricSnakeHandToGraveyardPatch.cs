using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Electric_Snake"/> / <see cref="Elephant_Statue_of_Blessing"/>: when sent from hand to the Graveyard, draw cards equal to printed <c>Mgc</c>.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdElectricSnakeHandToGraveyardPatch
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

        foreach ((CardModel card, PileType? from) in state)
        {
            if (from != PileType.Hand)
                continue;

            BaseMonsterCard? drawSource = card switch
            {
                Electric_Snake es => es,
                Elephant_Statue_of_Blessing el => el,
                _ => null
            };
            if (drawSource == null)
                continue;

            Player? player = drawSource.Owner;
            if (player?.Creature?.CombatState == null)
                continue;

            decimal n = drawSource.DynamicVars["Mgc"].BaseValue;
            if (n <= 0m)
                continue;

            var ctx = new BlockingPlayerChoiceContext();
            await CardPileCmd.Draw(ctx, n, player);
        }
    }
}
