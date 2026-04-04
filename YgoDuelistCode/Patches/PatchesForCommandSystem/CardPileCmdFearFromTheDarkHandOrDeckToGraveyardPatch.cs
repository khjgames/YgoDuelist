using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Fear_from_the_Dark"/>: when sent from hand or draw pile to the Graveyard, Special Summon it (see <see cref="DuelMonsterSummon.TrySummonDuelMonsterSpecial"/>).
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdFearFromTheDarkHandOrDeckToGraveyardPatch
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

        if (CombatManager.Instance is not { IsInProgress: true })
            return;

        foreach ((CardModel card, PileType? from) in state)
        {
            if (from is not (PileType.Hand or PileType.Draw) || card is not Fear_from_the_Dark fear)
                continue;

            Player? player = fear.Owner;
            if (player?.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
                continue;

            if (!GraveyardRelic.GetGraveyardCards(player).Contains(fear))
                continue;

            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, tributeReleaseCount: 0))
                continue;

            var ctx = new BlockingPlayerChoiceContext();
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, fear, ctx);
        }
    }
}
