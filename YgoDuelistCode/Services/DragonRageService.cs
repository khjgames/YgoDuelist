using System.Text;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class DragonRageService
{
    public static bool MonsterCardShowsSplinterFromDragonRage(BaseMonsterCard m)
    {
        if (m.IsCanonical)
            return false;

        return m is { Owner: { Creature: { } c } }
            && c.GetPower<DragonRagePower>() != null
            && m.GetEffectiveDuelMonsterRace() == DuelMonsterRace.Dragon;
    }

    public static bool AttackGetsSplinterFromDragonRageAura(BaseMonsterCard monster, Player? attackingPlayer)
    {
        if (monster == null || attackingPlayer?.Creature == null)
            return false;
        if (monster.GetEffectiveDuelMonsterRace() != DuelMonsterRace.Dragon)
            return false;
        return attackingPlayer.Creature.GetPower<DragonRagePower>() != null;
    }

    public static async Task SyncPlayerPowerAsync(Player? player)
    {
        if (player?.Creature?.CombatState == null)
            return;

        bool want = ShouldHaveAura(player);

        bool has = player.Creature.GetPower<DragonRagePower>() != null;
        if (want && !has)
            await PowerCmd.Apply<DragonRagePower>(player.Creature, 1m, player.Creature, null);
        else if (!want && has)
            await PowerCmd.Remove<DragonRagePower>(player.Creature);
    }

    public static string FormatDragonRageChecksumFingerprint(IRunState runState)
    {
        var sb = new StringBuilder();
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature?.CombatState == null)
                continue;

            bool want = ShouldHaveAura(player);
            bool has = player.Creature.GetPower<DragonRagePower>() != null;
            sb.Append($"[YgoDuelist][MP][DragonRage][fp] net={player.NetId} want={want} has={has}");

            CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
            if (zone != null)
            {
                foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards))
                {
                    if (c is not Dragon_s_Rage rage)
                        continue;
                    sb.Append($" | rageFd={rage.FaceDown}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static void ReconcileForChecksum(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature == null)
                continue;

            bool want = ShouldHaveAura(player);
            DragonRagePower? existing = player.Creature.GetPower<DragonRagePower>();
            bool has = existing != null;
            if (want == has)
                continue;

            if (!want && existing != null)
            {
                existing.RemoveInternal();
                GD.Print($"[YgoDuelist][MP][Checksum][DragonRage] Removed stale aura power owner={player.NetId}");
                continue;
            }

            if (want && !has)
            {
                PowerModel proto = ModelDb.Power<DragonRagePower>();
                PowerModel power = proto.ToMutable();
                power.Applier = player.Creature;
                power.ApplyInternal(player.Creature, 1m, silent: true);
                GD.Print($"[YgoDuelist][MP][Checksum][DragonRage] Applied missing aura power owner={player.NetId}");
            }
        }
    }

    private static bool ShouldHaveAura(Player player)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return false;

        return YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Dragon_s_Rage { FaceDown: false }) != null;
    }
}
