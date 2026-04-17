using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public interface IYgoOwnerBeforeTurnEndFlushBanishedEffect
{
    bool IsOwnerBeforeTurnEndFlushBanishedEffectActive();
    Task TryResolveOwnerBeforeTurnEndFlushBanishedEffectAsync(PlayerChoiceContext choiceContext, Player owner);
}
