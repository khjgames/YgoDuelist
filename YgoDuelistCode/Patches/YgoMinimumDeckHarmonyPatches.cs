using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(CardSelectCmd))]
public static class CardSelectCmdFromDeckForRemovalYgoMinimumPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(CardSelectCmd),
            nameof(CardSelectCmd.FromDeckForRemoval),
            new[] { typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>) });

    public static void Prefix(Player player, ref Func<CardModel, bool>? filter)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        int min = YgoPlayerMinimumDeck.Get(player);
        Func<CardModel, bool>? inner = filter;
        filter = c =>
        {
            if (player.Deck.Cards.Count <= min)
                return false;
            return inner == null || inner(c);
        };
    }
}

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
        if (p == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(p))
            return;

        __result = YgoDeckRemovalMinTracker.ChainAfterRemoveFromDeck(__result, cards);
    }
}
