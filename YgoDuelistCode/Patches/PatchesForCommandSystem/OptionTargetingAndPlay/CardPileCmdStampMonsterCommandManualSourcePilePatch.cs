using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.OnPlay"/> runs after the card is moved to the play pile, so vanilla <see cref="CardModel.Pile"/>
/// is no longer Hand/Draw/Discard/Exhaust. Stamp the pre-move pile on <see cref="MonsterCommandCard"/> first
/// (<see cref="HarmonyPriority"/> <see cref="Priority.First"/> so it runs before <see cref="CardPileCmdOptionPilePlayPatch"/>).
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
[HarmonyPriority(Priority.First)]
public static class CardPileCmdStampMonsterCommandManualSourcePilePatch
{
    static void Prefix(CardModel card)
    {
        if (card is MonsterCommandCard mcc)
            mcc.PendingManualPlaySourcePileType = card.Pile?.Type;
    }
}
