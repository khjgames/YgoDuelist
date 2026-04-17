using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Field monster that gains stacking ATK from <see cref="MegaCrit.Sts2.Core.Commands.PowerCmd"/> each player turn start after monster-command reset.
/// <see cref="Patches.MonsterCommandTurnResetPatch"/>.
/// </summary>
public interface IYgoTurnStartAtkGrowthFromFieldMonsterAfterCommandReset
{
    Task ApplyTurnStartAtkGrowthAsync(Player player, Creature pet);
}
