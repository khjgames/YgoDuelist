using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Optional field-monster effect hook resolved at the owner's turn start.
/// </summary>
public interface IYgoOwnerTurnStartFieldMonsterEffect
{
    bool IsOwnerTurnStartFieldMonsterEffectActive();
    Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner);
}
