using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Monster moved to GY from hand or draw pile — card-specific async follow-up (e.g. Special Summon self).
/// Dispatched from <see cref="YgoDuelist.YgoDuelistCode.Patches.CardPileCmdMonsterGraveyardFromHandOrDrawHookPatch"/>.
/// </summary>
public interface IYgoAfterMonsterMovedToGraveyardFromHandOrDraw
{
    Task OnAfterMovedToGraveyardFromHandOrDrawAsync(Player player, PileType fromPile);
}
