using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Face-up spell/trap zone card effect that may resolve at the owner's turn start.
/// Use <see cref="OwnerTurnStartSpellTrapDispatchPhase"/> to pick ordering relative to other <c>GraveyardRelic.AfterPlayerTurnStart</c> work.
/// </summary>
public interface IYgoOwnerTurnStartSpellTrapZoneEffect
{
    YgoOwnerTurnStartSpellTrapDispatchPhase OwnerTurnStartSpellTrapDispatchPhase =>
        YgoOwnerTurnStartSpellTrapDispatchPhase.AfterSealmasterBeforeFieldMonsterHooks;

    bool IsOwnerTurnStartSpellTrapZoneEffectActive();
    Task TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player player);
}
