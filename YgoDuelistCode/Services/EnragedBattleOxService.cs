using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
            && m.DuelMonsterRace == DuelMonsterRace.BeastWarrior;
    }

    public static bool AttackGetsSplinterFromOxAura(BaseMonsterCard monster, Player? attackingPlayer)
    {
        if (monster == null || attackingPlayer?.Creature == null)
            return false;
        if (monster.DuelMonsterRace != DuelMonsterRace.BeastWarrior)
            return false;
        return attackingPlayer.Creature.GetPower<EnragedBattleOxPower>() != null;
    }

    public static async Task SyncPlayerPowerAsync(Player? player)
    {
        if (player?.Creature?.CombatState == null)
            return;

        bool want = false;
        if (player.PlayerCombatState != null)
        {
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (!pet.IsAlive)
                    continue;
                if (DuelMonsterFieldRegistry.GetSourceMonster<Enraged_Battle_Ox>(pet) is not Enraged_Battle_Ox ox)
                    continue;
                if (ox.FaceDown)
                    continue;
                want = true;
                break;
            }
        }

        bool has = player.Creature.GetPower<EnragedBattleOxPower>() != null;
        if (want && !has)
            await PowerCmd.Apply<EnragedBattleOxPower>(player.Creature, 1m, player.Creature, null);
        else if (!want && has)
            await PowerCmd.Remove<EnragedBattleOxPower>(player.Creature);
    }
}
