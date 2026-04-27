using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Run-map deck removals: after vanilla <see cref="CardPileCmd.RemoveFromDeck(IReadOnlyList{CardModel}, bool)"/> completes,
/// defer one frame then lower minimum only if the card did not land in trunk/side/extra/main deck.
/// </summary>
[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.RemoveFromDeck),
    new[] { typeof(IReadOnlyList<CardModel>), typeof(bool) })]
public static class CardPileCmdRemoveFromDeckYgoMinimumPatch
{
    public static void Postfix(IReadOnlyList<CardModel> cards, ref Task __result)
    {
        if (cards == null || cards.Count == 0)
            return;

        Player? p = cards[0].Owner;
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(p))
            return;

        __result = YgoDeckRemovalMinTracker.ChainAfterRemoveFromDeck(__result, cards);
    }
}
