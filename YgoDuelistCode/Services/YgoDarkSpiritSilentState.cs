using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>One combat round of <see cref="Cards.Trap.Todo.Normal.Dark_Spirit_of_the_Silent"/> (cleared at the start of the trap owner's next turn).</summary>
public static class YgoDarkSpiritSilentState
{
    private sealed class Entry
    {
        public uint StunnedCombatId;
        public uint DoubleHitCombatId;
    }

    private static readonly Dictionary<Player, Entry> ByPlayer = new();

    public static void ClearAll() => ByPlayer.Clear();

    public static void ClearForPlayer(Player player) => ByPlayer.Remove(player);

    public static void Activate(Player player, Creature stunnedTarget, Creature doubleHitTarget)
    {
        if (!stunnedTarget.CombatId.HasValue || !doubleHitTarget.CombatId.HasValue)
            return;

        ByPlayer[player] = new Entry
        {
            StunnedCombatId = stunnedTarget.CombatId.Value,
            DoubleHitCombatId = doubleHitTarget.CombatId.Value
        };
    }

    public static bool ShouldDoubleAttack(Player? player, Creature? attacker)
    {
        if (player == null || attacker == null || !attacker.CombatId.HasValue)
            return false;
        if (!ByPlayer.TryGetValue(player, out Entry? e))
            return false;
        return attacker.CombatId.Value == e.DoubleHitCombatId;
    }

    /// <summary>Attack damage from the chosen "silent" target is negated for this round (stunned enemy).</summary>
    public static bool IsNegated(Player? player, Creature? dealer)
    {
        if (player == null || dealer == null || !dealer.CombatId.HasValue)
            return false;
        if (!ByPlayer.TryGetValue(player, out Entry? e))
            return false;
        return dealer.CombatId.Value == e.StunnedCombatId;
    }
}
