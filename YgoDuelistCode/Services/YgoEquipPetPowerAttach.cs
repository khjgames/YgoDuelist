using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Apply/remove DNA-style override powers on the pet for an equipped monster.</summary>
public static class YgoEquipPetPowerAttach
{
    public static async Task ApplyAttributeOverrideAsync(
        Player player,
        BaseMonsterCard monster,
        DuelMonsterAttribute attribute,
        CardModel source)
    {
        Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, monster);
        if (pet == null || !pet.IsAlive || player.Creature == null)
            return;

        await PowerCmd.Apply<DnaTransplantAttributeOverridePower>(pet, (int)attribute + 1, player.Creature, source);
    }

    public static async Task ApplyRaceOverrideAsync(
        Player player,
        BaseMonsterCard monster,
        DuelMonsterRace race,
        CardModel source)
    {
        Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, monster);
        if (pet == null || !pet.IsAlive || player.Creature == null)
            return;

        await PowerCmd.Apply<DnaSurgeryRaceOverridePower>(pet, (int)race + 1, player.Creature, source);
    }

    public static async Task ApplyBlightPercentAsync(
        Player player,
        BaseMonsterCard monster,
        decimal percent,
        CardModel source)
    {
        Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, monster);
        if (pet == null || !pet.IsAlive || player.Creature == null)
            return;

        await PowerCmd.Apply<SecretPassTreasuresBlightPower>(pet, percent, player.Creature, source);
    }

    public static async Task RemoveAttributeOverrideAsync(BaseMonsterCard monster)
    {
        Creature? pet = monster.Owner != null
            ? TributeSummonSelection.ResolvePetForFieldCard(monster.Owner, monster)
            : null;
        DnaTransplantAttributeOverridePower? p = pet?.GetPower<DnaTransplantAttributeOverridePower>();
        if (p != null)
            await PowerCmd.Remove(p);
    }

    public static async Task RemoveRaceOverrideAsync(BaseMonsterCard monster)
    {
        Creature? pet = monster.Owner != null
            ? TributeSummonSelection.ResolvePetForFieldCard(monster.Owner, monster)
            : null;
        DnaSurgeryRaceOverridePower? p = pet?.GetPower<DnaSurgeryRaceOverridePower>();
        if (p != null)
            await PowerCmd.Remove(p);
    }

    public static async Task RemoveBlightPercentAsync(BaseMonsterCard monster)
    {
        Creature? pet = monster.Owner != null
            ? TributeSummonSelection.ResolvePetForFieldCard(monster.Owner, monster)
            : null;
        SecretPassTreasuresBlightPower? p = pet?.GetPower<SecretPassTreasuresBlightPower>();
        if (p != null)
            await PowerCmd.Remove(p);
    }

    public static async Task ApplyProtectionsAsync(Player player, BaseMonsterCard monster, CardModel source)
    {
        Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, monster);
        if (pet == null || !pet.IsAlive)
            return;

        await YgoDuelMonsterProtectionSummon.ApplyMagicProtectionAsync(player, pet);
        await YgoDuelMonsterProtectionSummon.ApplyMonsterProtectionAsync(player, pet);
    }
}
