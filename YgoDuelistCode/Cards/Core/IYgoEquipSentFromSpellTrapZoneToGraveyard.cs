using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Equip spells that trigger when sent from the Spell/Trap zone to the graveyard (dispatched from zone detach patch).
/// </summary>
public interface IYgoEquipSentFromSpellTrapZoneToGraveyard
{
    Task OnSentFromSpellTrapZoneToGraveyardAsync(Player owner);
}
