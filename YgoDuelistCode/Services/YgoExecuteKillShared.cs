using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Helpers for execute-kill hooks on <see cref="BaseMonsterCard.OnEnemyExecutedByThisAttackAsync"/>.</summary>
public static class YgoExecuteKillShared
{
    /// <summary><see cref="DamageResult.TotalDamage"/> plus <see cref="DamageResult.OverkillDamage"/> (full hit size for execute math).</summary>
    public static int FullIncomingDamage(DamageResult r) => r.TotalDamage + r.OverkillDamage;

    /// <summary>True if this attack chain killed at least one enemy creature.</summary>
    public static bool AnyEnemyExecutedKill(AttackCommand command)
    {
        foreach (DamageResult r in command.Results)
        {
            if (r.Receiver.Side == CombatSide.Enemy && r.WasTargetKilled)
                return true;
        }

        return false;
    }

    public static bool PlayerControlsAtLeastTwoFiendsOnField(Player player)
    {
        int fiends = 0;
        foreach (BaseMonsterCard c in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (c.DuelMonsterRace == DuelMonsterRace.Fiend)
                fiends++;
        }

        return fiends >= 2;
    }

    /// <summary>Shinato / Des Volstgalph: half of killing blow damage as Blight to each living enemy.</summary>
    public static async Task ApplyHalfBlightToAllEnemiesOnExecuteKillAsync(
        AttackCommand command,
        BaseMonsterCard monster,
        CombatState cs)
    {
        foreach (DamageResult r in command.Results)
        {
            int hitDamage = FullIncomingDamage(r);
            if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled || hitDamage <= 0)
                continue;

            int blight = (int)decimal.Floor(hitDamage * 0.5m);
            if (blight <= 0)
                continue;

            foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
            {
                if (!enemy.IsAlive)
                    continue;
                await PowerCmd.Apply<BlightPower>(enemy, blight, command.Attacker, monster);
            }
        }
    }
}
