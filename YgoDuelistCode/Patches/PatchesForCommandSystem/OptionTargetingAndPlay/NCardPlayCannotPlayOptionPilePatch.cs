using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Same structure as PlayCardFromOptionPilePatch: hook the exact method the game calls when it would
/// display "you can't play this card" (FTUE check + dialogue). The game calls CannotPlayThisCardFtueCheck
/// from both NMouseCardPlay.StartAsync (after drag, when !CanPlay) and NCardPlay.TryPlayCard (when !CanPlayTargeting).
/// We Postfix here so we run at the same time; only in the specific scenario (card in option pile + Toggle/Exit)
/// do we run OnClickedOption.
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

        if (card is Exit_Monster_Options exit)
            TaskHelper.RunSafely(exit.OnClickedOption());
        else if (card is Command_Change_Battle_Position changePos)
            TaskHelper.RunSafely(changePos.OnClickedOption());
        else if (card is Command_Toggle_Tribute_Sacrifice tributeToggle)
            TaskHelper.RunSafely(tributeToggle.OnClickedOption());
        else if (card is Toggle_Die_For_You toggle)
            TaskHelper.RunSafely(toggle.OnClickedOption());
    }
}
