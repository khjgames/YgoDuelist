using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Runs YgoDuelist combat teardown and Ra end-of-combat rules inside <see cref="MegaCrit.Sts2.Core.Hooks.Hook.AfterCombatEnd"/>,
/// via <see cref="Relics.GraveyardRelic.AfterCombatEnd"/> so work is awaited before <see cref="Player.AfterCombatEnd"/> strips powers and piles.
/// </summary>
public static class YgoCombatEndLifecycle
{
    private static readonly object Gate = new();

    private static CombatState? _cleanupRanForCombat;

    private static readonly List<(Player Player, decimal Amount, CardModel Source)> s_pendingRaRebirth = new();

    /// <summary>Call from <see cref="Relics.GraveyardRelic.BeforeCombatStart"/> so the next combat can run cleanup again.</summary>
    public static void ResetDedupForNewCombat()
    {
        lock (Gate)
        {
            _cleanupRanForCombat = null;
            s_pendingRaRebirth.Clear();
        }
    }

    /// <summary>
    /// When the killing blow removes the last primary enemy, <see cref="CombatManager.IsEnding"/> is still true while
    /// <see cref="MegaCrit.Sts2.Core.Hooks.Hook.AfterAttack"/> runs; <see cref="MegaCrit.Sts2.Core.Commands.PowerCmd.Apply{T}"/> no-ops.
    /// Queue here and flush at combat end before rebirth/doomed resolution (after <c>IsInProgress</c> is cleared).
    /// </summary>
    public static void EnqueuePendingRaRebirth(Player player, decimal amount, CardModel cardSource)
    {
        if (player?.Creature == null || amount <= 0m || cardSource == null)
            return;

        lock (Gate)
        {
            s_pendingRaRebirth.Add((player, amount, cardSource));
        }
    }

    /// <summary>Idempotent per <see cref="CombatState"/> instance for one Hook batch.</summary>
    public static async Task RunEndOfCombatCleanupIfNeededAsync(CombatRoom room)
    {
        CombatState cs = room.CombatState;
        lock (Gate)
        {
            if (ReferenceEquals(_cleanupRanForCombat, cs))
                return;
            _cleanupRanForCombat = cs;
        }

        await FlushPendingRaRebirthsAsync();

        var ctx = YgoChoiceContexts.Blocking();
        foreach (Player p in cs.Players)
            await RaRebirthPower.ResolveCombatEndBeforeDoomedAsync(p);
        foreach (Player p in cs.Players)
            await RaDoomedPower.ResolveCombatEndDamageAsync(ctx, p);

        DuelMonsterFieldRegistry.ClearAll();
        YgoDuelistPassivePowerState.ClearAll();
        SevenWeaponsHunterState.ClearAll();
        MonsterCommandRegistry.ClearAll();
        YgoDarkSpiritSilentState.ClearAll();
        NormalSummonTracker.ClearAll();
        LegionFiendJesterSpellcasterConduit.ClearAll();
        ReactorSlimeSummonGate.ClearAll();
        YgoFushiohRichieSummonGate.ClearAll();
        YgoTotalDefenseShogunDeferredBlock.ClearAll();
        YgoPlayerCombatTurnStamp.ClearAll();
        YgoSoulOfPurityAndLightTurnPulseDedup.ClearAll();
        YgoSolarFlareDragonTurnPulseDedup.ClearAll();
        YgoSanganNameLock.ClearAll();
        YgoBattleDeathMarkedCards.ClearAll();
        TributeSummonPlayPayload.ClearAll();
        EquipSpellPlayPayload.ClearAll();
        RitualSpellPlayPayload.ClearAll();
        FusionSpellPlayPayload.ClearAll();
        TailorOfTheFicklePlayPayload.ClearAll();
        EmergencyProvisionsPlayPayload.ClearAll();
        ActivatedEffectTributeSelectionPayload.ClearAll();
        ObeliskActivatedTributePayload.ClearAll();
        RushReliablePlayPayload.ClearAll();
        RiryokuPlayPayload.ClearAll();
        SecretPassPlayPayload.ClearAll();
        FairyOfSpringReturnedEquipLock.ClearAll();
        YgoEquipSpellRegistry.ClearAll();
        YgoUnionLimboRegistry.ClearAll();
        YgoSpellTrapEquipLinkRegistry.ClearAll();
        YgoCurseOfDarknessSpellHook.ClearAll();
        YgoDesCounterblowThornsSync.ClearAll();

        foreach (Player p in cs.Players)
            await YgoBanishedService.RemoveAllFromCombat(p);
        foreach (Player p in cs.Players)
            await YgoLimboService.RemoveAllFromCombat(p);
    }

    private static async Task FlushPendingRaRebirthsAsync()
    {
        List<(Player Player, decimal Amount, CardModel Source)> batch;
        lock (Gate)
        {
            if (s_pendingRaRebirth.Count == 0)
                return;
            batch = new List<(Player, decimal, CardModel)>(s_pendingRaRebirth);
            s_pendingRaRebirth.Clear();
        }

        foreach ((Player player, decimal amount, CardModel source) in batch)
        {
            if (player.Creature == null || !player.Creature.CanReceivePowers)
                continue;
            await PowerCmd.Apply<RaRebirthPower>(player.Creature, amount, player.Creature, source);
        }
    }
}
