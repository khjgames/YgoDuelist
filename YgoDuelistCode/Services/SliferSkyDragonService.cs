using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class SliferSkyDragonService
{
    public static Slifer_the_Sky_Dragon? GetControllingSlifer(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive)
                continue;
            BaseMonsterCard? src = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (src is IYgoSliferSkyDragonFieldMonster)
                return (Slifer_the_Sky_Dragon)src;
        }

        return null;
    }

    public static async Task ApplySliferPressureToEnemyAsync(
        PlayerChoiceContext ctx,
        Player controller,
        Creature enemy,
        Slifer_the_Sky_Dragon slifer)
    {
        if (controller.Creature == null || !enemy.IsAlive || enemy.Side != CombatSide.Enemy)
            return;

        await PowerCmd.Apply<WeakPower>(enemy, 1m, controller.Creature, slifer);

        decimal strLoss = slifer.DynamicVars["Mgc2"].BaseValue;
        if (slifer.IsUpgraded)
            await PowerCmd.Apply<SlifersPressureTemporaryStrengthPowerPlus>(enemy, strLoss, controller.Creature, slifer);
        else
            await PowerCmd.Apply<SlifersPressureTemporaryStrengthPower>(enemy, strLoss, controller.Creature, slifer);
    }

    public static async Task ApplySliferPressureToAllEnemiesAsync(PlayerChoiceContext ctx, Player player)
    {
        Slifer_the_Sky_Dragon? slifer = GetControllingSlifer(player);
        if (slifer == null || player.Creature?.CombatState == null)
            return;

        foreach (Creature enemy in player.Creature.CombatState.HittableEnemies.ToList())
        {
            if (!enemy.IsAlive)
                continue;
            await ApplySliferPressureToEnemyAsync(ctx, player, enemy, slifer);
        }
    }

}
