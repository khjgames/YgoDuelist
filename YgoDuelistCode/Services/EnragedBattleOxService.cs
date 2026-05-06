using System.Text;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class EnragedBattleOxService
{
    public static bool MonsterCardShowsSplinterFromOx(BaseMonsterCard m)
    {
        // Canonical templates (card library, compendium) have no Owner; CardModel.Owner asserts mutable.
        if (m.IsCanonical)
            return false;

        return m is { Owner: { Creature: { } c } }
            && c.GetPower<EnragedBattleOxPower>() != null
            && m.GetEffectiveDuelMonsterRace() == DuelMonsterRace.BeastWarrior;
    }

    public static bool AttackGetsSplinterFromOxAura(BaseMonsterCard monster, Player? attackingPlayer)
    {
        if (monster == null || attackingPlayer?.Creature == null)
            return false;
        if (monster.GetEffectiveDuelMonsterRace() != DuelMonsterRace.BeastWarrior)
            return false;
        return attackingPlayer.Creature.GetPower<EnragedBattleOxPower>() != null;
    }

    public static async Task SyncPlayerPowerAsync(Player? player)
    {
        if (player?.Creature?.CombatState == null)
            return;

        bool want = ShouldHaveAura(player);

        bool has = player.Creature.GetPower<EnragedBattleOxPower>() != null;
        if (want && !has)
            await PowerCmd.Apply<EnragedBattleOxPower>(player.Creature, 1m, player.Creature, null);
        else if (!want && has)
            await PowerCmd.Remove<EnragedBattleOxPower>(player.Creature);
    }

    /// <summary>
    /// MP divergence aid: one line per player with ox-aura want/has plus each live Enraged Battle Ox field binding.
    /// Call from checksum mismatch handlers when context is <see cref="YgoMonsterCommandChecksumReconcile.AfterPlayerTurnStartContext"/>.
    /// </summary>
    public static string FormatOxAuraChecksumFingerprint(IRunState runState)
    {
        var sb = new StringBuilder();
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature?.CombatState == null)
                continue;

            bool want = ShouldHaveAura(player);
            bool has = player.Creature.GetPower<EnragedBattleOxPower>() != null;
            sb.Append($"[YgoDuelist][MP][EnragedOx][fp] net={player.NetId} want={want} has={has}");

            if (player.PlayerCombatState != null)
            {
                foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
                {
                    if (!pet.IsAlive)
                        continue;
                    if (DuelMonsterFieldRegistry.GetSourceMonster<Enraged_Battle_Ox>(pet) is not { } ox)
                        continue;
                    sb.Append($" | oxPet={pet.CombatId} fd={ox.FaceDown}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// MP checksum safety net: normalize Enraged Battle Ox aura marker before snapshot if async gameplay sync paths
    /// didn't complete on one peer yet.
    /// </summary>
    public static void ReconcileForChecksum(IRunState runState)
    {
        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature == null)
                continue;

            bool want = ShouldHaveAura(player);
            EnragedBattleOxPower? existing = player.Creature.GetPower<EnragedBattleOxPower>();
            bool has = existing != null;
            if (want == has)
                continue;

            if (!want && existing != null)
            {
                existing.RemoveInternal();
                GD.Print($"[YgoDuelist][MP][Checksum][EnragedOx] Removed stale aura power owner={player.NetId}");
                continue;
            }

            if (want && !has)
            {
                PowerModel proto = ModelDb.Power<EnragedBattleOxPower>();
                PowerModel power = proto.ToMutable();
                power.Applier = player.Creature;
                power.ApplyInternal(player.Creature, 1m, silent: true);
                GD.Print($"[YgoDuelist][MP][Checksum][EnragedOx] Applied missing aura power owner={player.NetId}");
            }
        }
    }

    private static bool ShouldHaveAura(Player player)
    {
        if (player.PlayerCombatState == null)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<Enraged_Battle_Ox>(pet) is not Enraged_Battle_Ox ox)
                continue;
            if (ox.FaceDown)
                continue;
            return true;
        }

        return false;
    }
}
