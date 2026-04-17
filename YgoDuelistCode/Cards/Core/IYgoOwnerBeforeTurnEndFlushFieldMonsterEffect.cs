using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public interface IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet);
    Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner, Creature pet);
}
