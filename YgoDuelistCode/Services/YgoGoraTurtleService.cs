using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Gora_Turtle"/>: at start of your turn, enemies whose attack intent vs you is at least this card's <c>Mgc</c> get 1 Weak.
/// </summary>
public static class YgoGoraTurtleService
{
    public static async Task ApplyPlayerTurnStartAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature?.CombatState == null)
            return;

        Gora_Turtle? source = null;
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is Gora_Turtle gora && !gora.FaceDown)
            {
                source = gora;
                break;
            }
        }

        if (source == null || source.Owner == null)
            return;

        int threshold = (int)source.DynamicVars["Mgc"].BaseValue;
        if (threshold <= 0)
            return;

        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        foreach (Creature enemy in player.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, player.Creature);
            if (intent < threshold)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, 1m, player.Creature, source);
        }
    }
}
