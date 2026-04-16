using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Field monster whose move to the graveyard after pet death is not the default equips+monster path
/// (<see cref="Patches.DuelMonsterPetDeathPatch.MoveEquipsToGraveyardThenMonsterToPileAsync"/>).
/// </summary>
public interface IYgoCustomFieldMonsterDeathGraveyardRelocation
{
    Task RunCustomFieldMonsterDeathGraveyardRelocationAsync(Player player, CardPile graveyard);
}
