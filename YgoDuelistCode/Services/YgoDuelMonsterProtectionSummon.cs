using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoDuelMonsterProtectionSummon
{
    public static async Task ApplyUnyieldingAsync(Player player, Creature pet, decimal stacks)
    {
        if (player?.Creature == null || pet == null || !pet.IsAlive || stacks <= 0m)
            return;
        await PowerCmd.Apply<UnyieldingPower>(pet, stacks, player.Creature, null);
    }

    public static async Task ApplyMagicProtectionAsync(Player player, Creature pet)
    {
        if (player?.Creature == null || pet == null || !pet.IsAlive)
            return;
        if (pet.HasPower<MagicProtectionKeywordPower>())
            return;
        await PowerCmd.Apply<MagicProtectionKeywordPower>(pet, 1m, player.Creature, null);
    }

    public static async Task ApplyMonsterProtectionAsync(Player player, Creature pet)
    {
        if (player?.Creature == null || pet == null || !pet.IsAlive)
            return;
        if (pet.HasPower<MonsterProtectionKeywordPower>())
            return;
        await PowerCmd.Apply<MonsterProtectionKeywordPower>(pet, 1m, player.Creature, null);
    }
}
