using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Marks the enemy bound by face-up <see cref="Spellbinding_Circle"/>: each of your turn starts, apply Spellbinding temp STR and +1 Spellbound.</summary>
public sealed class SpellbindingCircleTargetPower : YgoDuelistPower
{
    /// <summary>
    /// MP checksum: if <see cref="Spellbinding_Circle"/> left the zone on a player, strip marker + spellbinding temp STR
    /// from enemies so host/client agree (async <see cref="AfterPlayerTurnStart"/> timing can leave one peer stale).
    /// </summary>
    public static void ReconcileOrphansBeforeMpChecksum(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature == null)
                continue;

            CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
            if (zone != null && zone.Cards.OfType<Spellbinding_Circle>().Any())
                continue;

            RemoveAllSpellbindingChainForApplierSyncForChecksum(player.Creature);
        }
    }

    /// <summary>Removes <see cref="SpellbindingCircleTargetPower"/>, spellbinding temp STR, and <see cref="SpellboundPower"/> / <see cref="SpellboundPlusPower"/> applied by <paramref name="applier"/>.</summary>
    internal static void RemoveAllSpellbindingChainForApplierSyncForChecksum(Creature applier)
    {
        CombatState? cs = applier.CombatState;
        if (cs == null)
            return;

        foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
            RemoveSpellbindingChainOnCreatureSyncForChecksum(enemy, applier);
    }

    private static void RemoveSpellbindingChainOnCreatureSyncForChecksum(Creature enemy, Creature applier)
    {
        foreach (PowerModel p in enemy.Powers.ToList())
        {
            switch (p)
            {
                case SpellbindingCircleTargetPower m when m.Applier == applier:
                    m.RemoveInternal();
                    break;
                case SpellbindingTemporaryStrengthPower or SpellbindingTemporaryStrengthPowerPlus:
                    if (p.Applier == applier)
                        p.RemoveInternal();
                    break;
                case SpellboundPower:
                case SpellboundPlusPower:
                    if (p.Applier == applier)
                        p.RemoveInternal();
                    break;
            }
        }
    }

    public static async Task RemoveAllForApplier(Creature applier)
    {
        var cs = applier.CombatState;
        if (cs == null)
            return;
        foreach (Creature e in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
        {
            SpellbindingCircleTargetPower? p = e.GetPower<SpellbindingCircleTargetPower>();
            if (p != null && p.Applier == applier)
                await PowerCmd.Remove(p);
        }
    }

    public static void SyncCleanupIfTrapAbsent(Player player)
    {
        if (player?.Creature == null)
            return;
        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
        if (zone != null && zone.Cards.OfType<Spellbinding_Circle>().Any())
            return;
        TaskHelper.RunSafely(RemoveAllForApplier(player.Creature));
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SPELLBINDING_CIRCLE_TARGET_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPELLBINDING_CIRCLE_TARGET_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Applier?.Player)
            return;
        if (!Owner.IsAlive)
            return;

        Creature? applier = Applier;
        if (applier == null)
            return;
        Player? pl = applier.Player;
        if (pl == null)
            return;

        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(pl);
        Spellbinding_Circle? src = zone == null
            ? null
            : YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Spellbinding_Circle) as Spellbinding_Circle;
        if (src == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        decimal mgc = src.DynamicVars != null && src.DynamicVars.ContainsKey("Mgc")
            ? src.DynamicVars["Mgc"].BaseValue
            : 1m;
        if (mgc > 0m)
        {
            if (src.IsUpgraded)
                await PowerCmd.Apply<SpellbindingTemporaryStrengthPowerPlus>(Owner, mgc, applier, src);
            else
                await PowerCmd.Apply<SpellbindingTemporaryStrengthPower>(Owner, mgc, applier, src);
        }

        await AddSpellboundStacks(Owner, applier, src, 1m);
    }

    private static async Task AddSpellboundStacks(Creature enemy, Creature applier, CardModel src, decimal delta)
    {
        SpellboundPower? a = enemy.GetPower<SpellboundPower>();
        if (a != null)
        {
            await PowerCmd.ModifyAmount(a, delta, applier, src);
            return;
        }

        SpellboundPlusPower? b = enemy.GetPower<SpellboundPlusPower>();
        if (b != null)
            await PowerCmd.ModifyAmount(b, delta, applier, src);
    }
}
