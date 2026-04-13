using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Spell/Trap energy orbs read <see cref="CardModel.EnergyCost"/> hooks; nudge YGO cards when De-Spell / Fake Trap discount toggles.</summary>
public static class YgoSpellTrapDiscountEnergyRefresh
{
    public static void ForPlayer(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return;

        foreach (CardModel card in player.PlayerCombatState.AllCards)
        {
            if (card is BaseSpellCard or BaseTrapCard)
                card.InvokeEnergyCostChanged();
        }
    }
}
