using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Field → GY deck search (<see cref="IFieldToGraveyardDeckSearchEffect"/>), Twin-Headed Behemoth (field → GY end-phase setup), Twin mini-stats clear (field → elsewhere).
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdFieldMonsterGraveyardEffectsPatch
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
        if (__state == null)
            return;

        _ = AfterAddAsync(__result, newPile, __state);
    }

    private static async Task AfterAddAsync(Task moveCompleted, CardPile newPile, List<(CardModel Card, PileType? From)> state)
    {
        await moveCompleted;

        foreach ((CardModel card, PileType? from) in state)
        {
            Player? owner = card.Owner as Player;
            if (card is BaseMonsterCard bmMoved)
                bmMoved.OnAfterPileMoveCompleted(owner, from, newPile.Type);

            if (newPile.Type != GraveyardPile.CustomType || from != MonsterPile.CustomType)
                continue;

            if (card.Owner is not Player player)
                continue;

            if (card is IFieldToGraveyardDeckSearchEffect && card is BaseMonsterCard bmSearch)
                await TaskHelper.RunSafely(YgoFieldToGraveyardDeckSearch.OnSentFromFieldToGraveyardAsync(player, bmSearch));
        }
    }
}
