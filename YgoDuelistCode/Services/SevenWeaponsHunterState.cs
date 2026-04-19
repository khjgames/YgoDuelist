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

            await SyncSevenWeaponsOnPetAsync(pet, player.Creature, hw, stack);
        }
    }

    /// <summary>
    /// Keeps the counter in sync without removing/reapplying every time (that was firing power-off VFX every card).
    /// <see cref="PowerCmd.Apply{T}"/> adds <paramref name="stack"/> to an existing power, so we use
    /// <see cref="PowerCmd.ModifyAmount"/> with an explicit delta instead of Remove+Apply.
    /// </summary>
    private static async Task SyncSevenWeaponsOnPetAsync(Creature pet, Creature applier, The_Hunter_with_7_Weapons hw, int stack)
    {
        bool wantPlus = hw.IsUpgraded;
        SevenWeaponsPower? baseP = pet.GetPower<SevenWeaponsPower>();
        SevenWeaponsPlusPower? plusP = pet.GetPower<SevenWeaponsPlusPower>();

        if (wantPlus && baseP != null)
            await PowerCmd.Remove(baseP);
        if (!wantPlus && plusP != null)
            await PowerCmd.Remove(plusP);

        if (wantPlus)
            await SyncSevenWeaponsPowerStacksAsync<SevenWeaponsPlusPower>(pet, applier, hw, stack);
        else
            await SyncSevenWeaponsPowerStacksAsync<SevenWeaponsPower>(pet, applier, hw, stack);
    }

    private static async Task SyncSevenWeaponsPowerStacksAsync<T>(Creature pet, Creature applier, The_Hunter_with_7_Weapons hw, int stack)
        where T : PowerModel
    {
        T? power = pet.GetPower<T>();
        if (power == null)
        {
            // First apply: no fanfare until the weapon is "armed" at max stacks.
            bool silentApply = stack != SevenWeaponsPower.StacksForAtkBonus;
            await PowerCmd.Apply<T>(pet, stack, applier, hw, silentApply);
            return;
        }

        int prev = power.Amount;
        int delta = stack - prev;
        if (delta == 0)
            return;

        int max = SevenWeaponsPower.StacksForAtkBonus;
        bool silentDelta = !((prev == max && stack != max) || (prev != max && stack == max));
        await PowerCmd.ModifyAmount(power, delta, applier, hw, silentDelta);
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

        int amt = GetStackAmount(player);
        bool wantPlus = hw.IsUpgraded;

        if (wantPlus)
        {
            RemovePowerIfPresent<SevenWeaponsPower>(pet);
            if (pet.GetPower<SevenWeaponsPlusPower>() is { } plus)
                plus.SetAmount(amt, silent: true);
            else
                ApplyPowerSyncForChecksum<SevenWeaponsPlusPower>(pet, player.Creature, amt);
        }
        else
        {
            RemovePowerIfPresent<SevenWeaponsPlusPower>(pet);
            if (pet.GetPower<SevenWeaponsPower>() is { } sw)
                sw.SetAmount(amt, silent: true);
            else
                ApplyPowerSyncForChecksum<SevenWeaponsPower>(pet, player.Creature, amt);
        }
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
