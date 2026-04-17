using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Ancient Chant: when banished from the GY, apply Ra tribute buff on the player.</summary>
public interface IYgoApplyAncientChantPowerWhenBanishedFromGraveyard
{
    Task ApplyPowerWhenBanishedFromGraveyardAsync(Player player);
}
