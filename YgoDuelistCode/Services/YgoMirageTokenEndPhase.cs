using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>End Phase: destroy <see cref="Mirage_Token"/>s created by <see cref="YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal.Physical_Double"/>.</summary>
public static class YgoMirageTokenEndPhase
{
    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in player.PlayerCombatState.Pets.ToList())
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not Mirage_Token mirage || !mirage.MirageDestroyAtEndOfTurn)
                continue;
            await CreatureCmd.Kill(pet, force: true);
        }
    }
}
