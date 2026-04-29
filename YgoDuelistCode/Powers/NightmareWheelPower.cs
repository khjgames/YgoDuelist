using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

internal static class NightmareWheelPowerShared
{
    /// <summary>
    /// MP checksum: if <see cref="Nightmare_Wheel"/> left the zone, strip debuff from enemies so host/client agree
    /// (async <see cref="NightmareWheelPower.AfterPlayerTurnStart"/> timing can leave one peer stale).
    /// </summary>
    public static void ReconcileOrphansBeforeMpChecksum(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature == null)
                continue;

            CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
            if (zone != null && zone.Cards.OfType<Nightmare_Wheel>().Any())
                continue;

            RemoveAllNightmareWheelForApplierSyncForChecksum(player.Creature);
        }
    }

    private static void RemoveAllNightmareWheelForApplierSyncForChecksum(Creature applier)
    {
        CombatState? cs = applier.CombatState;
        if (cs == null)
            return;

        foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
        {
            foreach (PowerModel p in enemy.Powers.ToList())
            {
                if (p is NightmareWheelPower or NightmareWheelPlusPower)
                {
                    if (p.Applier == applier)
                        p.RemoveInternal();
                }
            }
        }
    }

    public static async Task RemoveAllForApplier(Creature applier)
    {
        CombatState? cs = applier.CombatState;
        if (cs == null)
            return;
        foreach (Creature e in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
        {
            NightmareWheelPower? a = e.GetPower<NightmareWheelPower>();
            if (a != null && a.Applier == applier)
                await PowerCmd.Remove(a);
            NightmareWheelPlusPower? b = e.GetPower<NightmareWheelPlusPower>();
            if (b != null && b.Applier == applier)
                await PowerCmd.Remove(b);
        }
    }

    public static void SyncCleanupIfTrapAbsent(Player player)
    {
        if (player?.Creature == null)
            return;
        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
        if (zone != null && zone.Cards.OfType<Nightmare_Wheel>().Any())
            return;
        TaskHelper.RunSafely(RemoveAllForApplier(player.Creature));
    }

    public static async Task DestroyFaceUpTrapIfAnyAsync(Creature? applier)
    {
        Player? player = applier?.Player;
        if (player == null)
            return;

        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
        Nightmare_Wheel? trap = zone == null
            ? null
            : YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Nightmare_Wheel) as Nightmare_Wheel;
        if (trap == null || trap.Pile?.Type != SpellTrapZonePile.CustomType)
            return;

        CardPile? gy = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { trap },
            gy,
            CardPilePosition.Top,
            trap,
            false);
        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
    }

    public static async Task TickDamageAsync(
        YgoDuelistPower self,
        PlayerChoiceContext choiceContext,
        Player player,
        decimal turnDamage)
    {
        if (player != self.Applier?.Player)
            return;
        if (!self.Owner.IsAlive)
            return;

        Creature? applier = self.Applier;
        if (applier == null)
            return;
        Player? pl = applier.Player;
        if (pl == null)
            return;

        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(pl);
        Nightmare_Wheel? trap = zone == null
            ? null
            : YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Nightmare_Wheel) as Nightmare_Wheel;
        if (trap == null)
        {
            await PowerCmd.Remove(self);
            return;
        }

        if (turnDamage > 0m)
            await CreatureCmd.Damage(choiceContext, self.Owner, turnDamage, ValueProp.Unpowered, applier, trap);
    }
}

/// <summary>Debuff from base <see cref="Nightmare_Wheel"/>: fixed turn damage on the power. Trap must still be face-up in zone.</summary>
public sealed class NightmareWheelPower : YgoDuelistPower
{
    private const decimal TurnDamage = 5m;

    public static Task RemoveAllForApplier(Creature applier) => NightmareWheelPowerShared.RemoveAllForApplier(applier);

    public static void SyncCleanupIfTrapAbsent(Player player) => NightmareWheelPowerShared.SyncCleanupIfTrapAbsent(player);

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Mgc", TurnDamage) };

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-NIGHTMARE_WHEEL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-NIGHTMARE_WHEEL_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-NIGHTMARE_WHEEL_POWER.smartDescription";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) =>
        await NightmareWheelPowerShared.TickDamageAsync(this, choiceContext, player, TurnDamage);

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner == null || oldOwner.IsAlive)
            return;
        await NightmareWheelPowerShared.DestroyFaceUpTrapIfAnyAsync(Applier);
    }
}

/// <summary>Debuff from upgraded <see cref="Nightmare_Wheel"/>: higher fixed turn damage.</summary>
public sealed class NightmareWheelPlusPower : YgoDuelistPower
{
    private const decimal TurnDamage = 9m;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Mgc", TurnDamage) };

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-NIGHTMARE_WHEEL_PLUS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-NIGHTMARE_WHEEL_PLUS_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-NIGHTMARE_WHEEL_PLUS_POWER.smartDescription";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) =>
        await NightmareWheelPowerShared.TickDamageAsync(this, choiceContext, player, TurnDamage);

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner == null || oldOwner.IsAlive)
            return;
        await NightmareWheelPowerShared.DestroyFaceUpTrapIfAnyAsync(Applier);
    }
}
