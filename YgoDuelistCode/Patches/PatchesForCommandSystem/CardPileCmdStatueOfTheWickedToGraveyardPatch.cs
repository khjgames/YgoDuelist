using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Statue_of_the_Wicked"/>: when this face-down set trap is destroyed and sent from the Spell/Trap zone to the Graveyard, Special Summon 1 Wicked Token.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdStatueOfTheWickedToGraveyardPatch
{
    [HarmonyPrefix]
    public static void Prefix(IEnumerable<CardModel> cards, CardPile newPile, ref List<(CardModel Card, PileType? From, bool WasFaceDownSetTrap)>? __state)
    {
        __state = new List<(CardModel, PileType?, bool)>();
        foreach (CardModel c in cards)
        {
            bool fd = c is BaseTrapCard t && t.WasSetIntoSpellTrapZone && t.FaceDown;
            __state.Add((c, c.Pile?.Type, fd));
        }
    }

    [HarmonyPostfix]
    public static void Postfix(Task __result, CardPile newPile, List<(CardModel Card, PileType? From, bool WasFaceDownSetTrap)>? __state)
    {
        if (__state == null || newPile.Type != GraveyardPile.CustomType)
            return;

        _ = AfterGraveyardAddAsync(__result, __state);
    }

    private static async Task AfterGraveyardAddAsync(
        Task moveCompleted,
        List<(CardModel Card, PileType? From, bool WasFaceDownSetTrap)> state)
    {
        await moveCompleted;

        foreach ((CardModel card, PileType? from, bool wasFdSet) in state)
        {
            if (from != SpellTrapZonePile.CustomType || card is not Statue_of_the_Wicked || !wasFdSet)
                continue;

            Player? player = card.Owner;
            if (player?.Creature?.CombatState == null)
                continue;

            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                continue;

            var ctx = new BlockingPlayerChoiceContext();
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Wicked_Token>(player, ctx, defensePosition: false);
        }
    }
}
