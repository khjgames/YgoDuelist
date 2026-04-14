using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="The_Wicked_Worm_Beast"/>: during the End Phase, face-up copies return to the hand (pet dies with bounce-to-hand).
/// </summary>
public static class YgoWickedWormBeastEndPhase
{
    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in player.PlayerCombatState.Pets.ToList())
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not The_Wicked_Worm_Beast worm)
                continue;
            if (worm.FaceDown)
                continue;

            YgoDuelMonsterBounceToHand.RegisterForHandReturn(pet);
            await CreatureCmd.Kill(pet, force: true);
        }
    }
}
