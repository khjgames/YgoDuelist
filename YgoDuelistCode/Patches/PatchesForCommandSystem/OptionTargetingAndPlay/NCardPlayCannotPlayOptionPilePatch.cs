using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.GameActions;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Same structure as PlayCardFromOptionPilePatch: hook the exact method the game calls when it would
/// display "you can't play this card" (FTUE check + dialogue). The game calls CannotPlayThisCardFtueCheck
/// from both NMouseCardPlay.StartAsync (after drag, when !CanPlay) and NCardPlay.TryPlayCard (when !CanPlayTargeting).
/// Unplayable menu cards never enqueue <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction"/>; route through
/// <see cref="YgoMonsterMenuCommandGameAction"/> so host and clients apply the same state (see <see cref="YgoMonsterMenuCommandNetHelper"/>).
/// </summary>
[HarmonyPatch(typeof(NCardPlay), "CannotPlayThisCardFtueCheck")]
public static class NCardPlayCannotPlayOptionPilePatch
{
    static void Postfix(CardModel card)
    {
        if (card == null)
            return;

        var player = card.Owner;
        if (player == null)
            return;

        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null || card.Pile != optionPile)
            return;

        if (card is not Exit_Monster_Options && card is not Command_Change_Battle_Position && card is not Toggle_Die_For_You)
            return;

        YgoMonsterMenuCommandNetHelper.TryEnqueueOrRunLocal(card, enemyTarget: null);
    }
}
