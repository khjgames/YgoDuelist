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
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Monster moved to Graveyard from hand, draw pile, or discard pile: dispatches <see cref="IYgoAfterMonsterMovedToGraveyardFromHandOrDraw"/>.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdMonsterGraveyardFromHandOrDrawHookPatch
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
            if (from is not (PileType.Hand or PileType.Draw or PileType.Discard))
                continue;
            if (card is not IYgoAfterMonsterMovedToGraveyardFromHandOrDraw hook)
                continue;

            Player? player = card.Owner;
            if (player?.Creature?.CombatState == null)
                continue;

            await hook.OnAfterMovedToGraveyardFromHandOrDrawAsync(player, from!.Value);
        }
    }
}
