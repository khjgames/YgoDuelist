using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Optional pre-play step for monster Activate Effect commands.
/// Return false to cancel before resources are spent and before OnPlay runs.
/// </summary>
public interface IMonsterActivatedEffectPrePlaySelection
{
    Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source);
}
