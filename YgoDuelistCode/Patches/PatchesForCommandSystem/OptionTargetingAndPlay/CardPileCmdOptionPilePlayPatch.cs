using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When a card in the option pile is "played" (Command Defend, Command Attack), the game calls
/// CardPileCmd.AddDuringManualCardPlay which moves the card to the play pile and reparents the NCard
/// from our second-hand holder to the play container. That breaks the layout (holder stuck, or another
/// card becomes a dead node). So we skip the move entirely: the card never leaves the option pile,
/// the NCard stays in our holder, and no layout break.
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
public static class CardPileCmdOptionPilePlayPatch
{
    static bool Prefix(CardModel card, ref Task __result)
    {
        if (card?.Owner == null)
            return true;

        var optionPile = YgoPlayerPiles.OptionPile(card.Owner);
        var spellTrapPile = YgoPlayerPiles.SpellTrapZone(card.Owner);
        bool inOptionPile = optionPile != null && card.Pile == optionPile;
        bool inSpellTrapPile = spellTrapPile != null && card.Pile == spellTrapPile;
        if (!inOptionPile && !inSpellTrapPile)
            return true;

        __result = Task.CompletedTask;
        return false;
    }
}
