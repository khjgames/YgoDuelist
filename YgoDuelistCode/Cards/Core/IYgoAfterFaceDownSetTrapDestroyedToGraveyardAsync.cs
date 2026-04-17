using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Face-down set trap in Spell/Trap zone destroyed → Graveyard: card-specific resolution (e.g. token summon).
/// Dispatched from <see cref="YgoDuelist.YgoDuelistCode.Patches.CardPileCmdFaceDownSetTrapToGraveyardHookPatch"/>.
/// </summary>
public interface IYgoAfterFaceDownSetTrapDestroyedToGraveyardAsync
{
    Task OnAfterFaceDownSetTrapDestroyedToGraveyardAsync(Player player);
}
