using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// MP: before <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetFullCombatState"/> checksum serialization, align
/// equip spell / equip-link trap registries and <see cref="YgoDuelistPower"/> instances with authoritative combat state
/// (orphan trap debuffs for Spellbinding Circle / Nightmare Wheel, then <see cref="YgoDuelistPower.ReconcileForMpChecksumSnapshot"/>).
/// Extends the stance / Die For You pattern in <see cref="Patches.PatchesForMultiplayer.NetFullCombatStateYgoChecksumPatch"/>.
/// </summary>
public static class YgoDuelistPowerChecksumReconcile
{
    /// <summary>
    /// Run all YGO reconciles: equip + equip-link registries, spell trap orphans, then per-power <see cref="YgoDuelistPower.ReconcileForMpChecksumSnapshot"/>.
    /// </summary>
    /// <param name="verboseRegistryLog">When true, logs equip/link registry detach/rebind counts if any correction ran.</param>
    public static void ReconcileAll(IRunState runState, bool verboseRegistryLog = false)
    {
        (int eqDetached, int eqRebound) = YgoEquipSpellRegistry.ReconcileOrphansBeforeMpChecksum(runState);
        (int lkDetached, int lkRebound) = YgoSpellTrapEquipLinkRegistry.ReconcileOrphansBeforeMpChecksum(runState);
        if (verboseRegistryLog && (eqDetached != 0 || eqRebound != 0 || lkDetached != 0 || lkRebound != 0))
            GD.Print($"[YgoDuelist][MP][Checksum][reg] equip detach={eqDetached} rebind={eqRebound} | linkTrap detach={lkDetached} rebind={lkRebound}");

        SpellbindingCircleTargetPower.ReconcileOrphansBeforeMpChecksum(runState);
        NightmareWheelPowerShared.ReconcileOrphansBeforeMpChecksum(runState);

        CombatState? cs = CombatManager.Instance?.DebugOnlyGetState();
        if (cs == null)
            return;

        foreach (Creature c in cs.Creatures.ToList())
        {
            if (c == null || !c.IsAlive)
                continue;

            NormalizeSingletonYgoPowers(c);

            foreach (PowerModel p in c.Powers.ToList())
            {
                if (p is YgoDuelistPower ygo)
                    ygo.ReconcileForMpChecksumSnapshot(c);
            }
        }
    }

    private static void NormalizeSingletonYgoPowers(Creature creature)
    {
        var duplicateGroups = creature.Powers
            .OfType<YgoDuelistPower>()
            .Where(p => !p.IsInstanced && (p.StackType == PowerStackType.Single || p.StackType == PowerStackType.None))
            .GroupBy(p => p.GetType())
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var copies = group.Cast<PowerModel>().ToList();
            for (int i = 1; i < copies.Count; i++)
                copies[i].RemoveInternal();

            GD.Print(
                $"[YgoDuelist][MP][Checksum][powers] Removed duplicate singleton YGO powers " +
                $"owner={creature.Monster?.Id?.Entry ?? "?"} power={group.Key.Name} copies={copies.Count}");
        }
    }
}
