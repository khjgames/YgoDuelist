using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Combat-scoped count of cards played by each player; drives 1–7 stacks on <see cref="SevenWeaponsPower"/> /
/// <see cref="SevenWeaponsPlusPower"/> for <see cref="The_Hunter_with_7_Weapons"/>.
/// </summary>
public static class SevenWeaponsHunterState
{
    private static readonly Dictionary<Player, int> CardsPlayedThisCombat = new();

    public static void ClearAll() => CardsPlayedThisCombat.Clear();

    /// <summary>Increments once per <see cref="MegaCrit.Sts2.Core.Hooks.Hook.AfterCardPlayed"/> for that player.</summary>
    public static void RegisterCardPlayed(Player player)
    {
        if (player == null)
            return;
        CardsPlayedThisCombat.TryGetValue(player, out int n);
        CardsPlayedThisCombat[player] = n + 1;
    }

    /// <summary>
    /// 1–7 from cards played this combat (same mapping as the former Graveyard relic counter):
    /// 0 plays → 1; then step = <c>n % 7</c> except 7th/14th/… play → 7.
    /// </summary>
    public static int GetStackAmount(Player player)
    {
        if (player == null)
            return 1;
        CardsPlayedThisCombat.TryGetValue(player, out int n);
        if (n == 0)
            return 1;
        int m = n % 7;
        return m == 0 ? 7 : m;
    }

    public static async Task SyncHunterPetsAsync(Player? player)
    {
        if (player?.PlayerCombatState == null || player.Creature == null)
            return;

        int stack = GetStackAmount(player);

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not The_Hunter_with_7_Weapons hw)
                continue;

            await PowerCmd.Remove<SevenWeaponsPower>(pet);
            await PowerCmd.Remove<SevenWeaponsPlusPower>(pet);

            if (hw.IsUpgraded)
                await PowerCmd.Apply<SevenWeaponsPlusPower>(pet, stack, player.Creature, hw);
            else
                await PowerCmd.Apply<SevenWeaponsPower>(pet, stack, player.Creature, hw);
        }
    }

    /// <summary>MP checksum: align Hunter 7 Weapons stacks with <see cref="GetStackAmount"/> (no <see cref="PowerCmd"/>).</summary>
    public static void ApplySyncForChecksumIfHunter(Creature pet, Player player)
    {
        if (CombatManager.Instance?.IsEnding == true)
            return;
        if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not The_Hunter_with_7_Weapons hw)
            return;
        if (player?.Creature == null)
            return;

        RemovePowerIfPresent<SevenWeaponsPower>(pet);
        RemovePowerIfPresent<SevenWeaponsPlusPower>(pet);

        int amt = GetStackAmount(player);
        if (hw.IsUpgraded)
            ApplyPowerSyncForChecksum<SevenWeaponsPlusPower>(pet, player.Creature, amt);
        else
            ApplyPowerSyncForChecksum<SevenWeaponsPower>(pet, player.Creature, amt);
    }

    private static void RemovePowerIfPresent<T>(Creature pet) where T : PowerModel
    {
        T? p = pet.GetPower<T>();
        if (p != null)
            p.RemoveInternal();
    }

    private static void ApplyPowerSyncForChecksum<T>(Creature pet, Creature applier, int amount) where T : PowerModel
    {
        PowerModel proto = ModelDb.Power<T>();
        PowerModel power = proto.ToMutable();
        power.Applier = applier;
        power.ApplyInternal(pet, amount, silent: true);
    }
}
