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
/// Face-down set trap in Spell/Trap zone destroyed → Graveyard: dispatches <see cref="IYgoAfterFaceDownSetTrapDestroyedToGraveyardAsync"/>.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdFaceDownSetTrapToGraveyardHookPatch
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
            if (from != SpellTrapZonePile.CustomType || !wasFdSet)
                continue;
            if (card is not IYgoAfterFaceDownSetTrapDestroyedToGraveyardAsync hook)
                continue;

            Player? player = card.Owner;
            if (player?.Creature?.CombatState == null)
                continue;

            await hook.OnAfterFaceDownSetTrapDestroyedToGraveyardAsync(player);
        }
    }
}
