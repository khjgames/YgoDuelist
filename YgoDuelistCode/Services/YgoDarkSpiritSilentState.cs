using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>One combat round of <see cref="Cards.Trap.Todo.Normal.Dark_Spirit_of_the_Silent"/> (cleared at the start of the trap owner's next turn).</summary>
public static class YgoDarkSpiritSilentState
{
    private sealed class Entry
    {
        public uint NegatedCombatId;
        public HashSet<uint> DoubleHitCombatIds = [];
    }

    private static readonly Dictionary<Player, Entry> ByPlayer = new();

    public static void ClearAll() => ByPlayer.Clear();

    public static void ClearForPlayer(Player player) => ByPlayer.Remove(player);

    public static void Activate(Player player, Creature negated, CombatState cs)
    {
        if (!negated.CombatId.HasValue)
            return;

        var entry = new Entry { NegatedCombatId = negated.CombatId.Value };
        Creature playerCreature = player.Creature!;
        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive && c != negated))
        {
            if (!e.CombatId.HasValue)
                continue;
            if (YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, playerCreature) > 0)
                entry.DoubleHitCombatIds.Add(e.CombatId.Value);
        }

        ByPlayer[player] = entry;
    }

    public static bool IsNegated(Player? player, Creature? dealer)
    {
        if (player == null || dealer == null || !dealer.CombatId.HasValue)
            return false;
        if (!ByPlayer.TryGetValue(player, out Entry? e))
            return false;
        return dealer.CombatId.Value == e.NegatedCombatId;
    }

    public static bool ShouldDoubleAttack(Player? player, Creature? attacker)
    {
        if (player == null || attacker == null || !attacker.CombatId.HasValue)
            return false;
        if (!ByPlayer.TryGetValue(player, out Entry? e))
            return false;
        return e.DoubleHitCombatIds.Contains(attacker.CombatId.Value);
    }
}
