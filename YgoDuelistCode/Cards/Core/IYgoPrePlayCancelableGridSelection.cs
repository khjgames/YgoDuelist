using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Spell/trap cards that need a cancelable, confirm-style grid <em>before</em> the play spends energy.
/// Selection is stored via YgoPrePlaySelectedCardPayload and consumed in OnSpellPlay / OnTrapPlay.
/// Wired by Harmony patch PlayCardActionPrePlayCancelableGridPatch.
/// </summary>
public interface IYgoPrePlayCancelableGridSelection
{
    Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard);
}
