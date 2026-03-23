using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Handles "unplayable click" activation for set-mode spell/trap cards in hand.
/// </summary>
[HarmonyPatch(typeof(NCardPlay), "CannotPlayThisCardFtueCheck")]
public static class NCardPlayCannotPlaySpellTrapSetPatch
{
    static void Postfix(CardModel card)
    {
        if (card == null || card.Owner == null)
            return;
        if (card.Pile?.Type != PileType.Hand)
            return;

        bool shouldSet =
            card is BaseTrapCard ||
            (card is BaseSpellCard spell && spell.IsSetModeInHand);

        if (!shouldSet)
            return;

        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(card.Owner, card))
            return;

        TaskHelper.RunSafely(YgoSpellTrapZoneBridge.TrySetFromHandAsync(card));
    }
}
