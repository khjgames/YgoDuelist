using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class LevelModifierPowerSync
{
    public static int GetCostDownHandLevelReduction(Player? player)
    {
        CostDownHandLevelPower? power = player?.Creature?.GetPower<CostDownHandLevelPower>();
        if (power == null)
            return 0;
        int stacks = (int)power.Amount;
        if (stacks <= 0)
            return 0;
        return stacks * CostDownHandLevelPower.LevelReduction;
    }

    public static async Task SyncSummonedMonsterCostDownPowerAsync(
        Player player,
        Creature pet,
        BaseMonsterCard sourceCard,
        bool summonedFromHand)
    {
        if (!summonedFromHand)
            return;
        int reduction = GetCostDownHandLevelReduction(player);
        await SyncCounterPowerAmountAsync<CostDownSummonedLevelPower>(pet, reduction, player);
    }

    public static async Task SyncFieldLevelModifierPowersAndHpAsync(Player player)
    {
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard source)
                continue;

            int legendaryOceanReduction = GetLegendaryOceanLevelReduction(player, source);
            await SyncCounterPowerAmountAsync<LegendaryOceanLevelPower>(pet, legendaryOceanReduction, player);
        }

        await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);
    }

    private static int GetLegendaryOceanLevelReduction(Player player, BaseMonsterCard source)
    {
            if (source.GetEffectiveDuelMonsterAttribute() != DuelMonsterAttribute.Water)
            return 0;

        int reduction = 0;
        foreach (A_Legendary_Ocean ocean in YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(player).OfType<A_Legendary_Ocean>())
            reduction += (int)ocean.DynamicVars["Mgc2"].BaseValue;
        return reduction;
    }

    private static async Task SyncCounterPowerAmountAsync<TPower>(Creature target, int desiredAmount, Player player)
        where TPower : MegaCrit.Sts2.Core.Models.PowerModel
    {
        TPower? existing = target.GetPower<TPower>();
        if (desiredAmount <= 0)
        {
            if (existing != null)
                await PowerCmd.Remove<TPower>(target);
            return;
        }

        if (existing == null)
        {
            await PowerCmd.Apply<TPower>(target, desiredAmount, player.Creature, null);
            return;
        }

        int delta = desiredAmount - (int)existing.Amount;
        if (delta != 0)
            await PowerCmd.ModifyAmount(existing, delta, player.Creature, null);
    }
}
