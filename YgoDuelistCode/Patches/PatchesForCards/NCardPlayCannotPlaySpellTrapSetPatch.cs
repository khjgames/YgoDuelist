using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.GameActions;
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

        // UI-only TrySetFromHandAsync only ran on the local peer → MP desync. Enqueue a GameAction so host
        // and client both run the same CardPileCmd path (see YgoSetSpellTrapFromHandGameAction).
        if (YgoNetCombatActionRouter.IsMultiplayerCombatQueueActive)
        {
            NetCombatCard net = NetCombatCard.FromModel(card);
            byte ord = YgoSetSpellTrapFromHandGameAction.ComputeSameIdHandOrdinal(card, card.Owner);
            var gameAction = new YgoSetSpellTrapFromHandGameAction(card.Owner, net, card.Id, ord);
            if (YgoNetCombatActionRouter.TryRequestEnqueue(gameAction, "SetSpellTrapFromHand"))
                return;
        }

        TaskHelper.RunSafely(YgoSpellTrapZoneBridge.TrySetFromHandAsync(card));
    }
}
