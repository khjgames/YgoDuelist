using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoOwnerTurnStartFieldMonsterHooks
{
    public static async Task TryResolvePlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<IYgoOwnerTurnStartFieldMonsterEffect>(pet) is not IYgoOwnerTurnStartFieldMonsterEffect hook)
                continue;
            if (!hook.IsOwnerTurnStartFieldMonsterEffectActive())
                continue;
            await hook.TryResolveOwnerTurnStartFieldMonsterEffectAsync(choiceContext, player);
        }
    }
}
