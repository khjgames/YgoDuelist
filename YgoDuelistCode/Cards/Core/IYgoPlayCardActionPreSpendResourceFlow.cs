using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Normal / Quick-Play spells that need a grid or extra confirmation after enqueue but before
/// <see cref="CardModel.SpendResources"/>. Wired by patch PlayCardActionIYgoPreSpendResourceFlowPatch.
/// </summary>
public interface IYgoPlayCardActionPreSpendResourceFlow
{
    /// <summary>
    /// Run prompts and set payload state for <c>OnSpellPlay</c>. Return false if cancelled.
    /// </summary>
    Task<bool> TryPreparePreSpendPlayAsync(PlayCardAction action, Player player, CardModel self);

    /// <summary>
    /// Clear any pending payload if the play was cancelled or finished; called from the patch <c>finally</c>.
    /// </summary>
    void ClearPreSpendPlayState(CardModel self);
}
