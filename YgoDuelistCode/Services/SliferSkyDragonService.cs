using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class SliferSkyDragonService
{
    public static Slifer_the_Sky_Dragon? GetControllingSlifer(Player? player)
    {
        foreach (Slifer_the_Sky_Dragon slifer in GetControllingSlifers(player))
            return slifer;
        return null;
    }

    public static IEnumerable<Slifer_the_Sky_Dragon> GetControllingSlifers(Player? player)
    {
        if (player?.PlayerCombatState == null)
            yield break;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is Slifer_the_Sky_Dragon slifer
                && slifer is IYgoSliferSkyDragonFieldMonster
                && !slifer.FaceDown)
                yield return slifer;
        }
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

    public static async Task ApplySliferPressureFromSliferToAllEnemiesAsync(
        PlayerChoiceContext ctx,
        Player player,
        Slifer_the_Sky_Dragon slifer)
    {
        if (player.Creature?.CombatState == null)
            return;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(player.Creature.CombatState))
        {
            await ApplySliferPressureToEnemyAsync(ctx, player, enemy, slifer);
        }
    }

    public static async Task ApplyAllSliferPressureToEnemyAsync(
        PlayerChoiceContext ctx,
        Player player,
        Creature enemy)
    {
        foreach (Slifer_the_Sky_Dragon slifer in GetControllingSlifers(player))
        {
            await ApplySliferPressureToEnemyAsync(ctx, player, enemy, slifer);
        }
    }
}
