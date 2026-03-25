using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Maps trap owner to the enemy <see cref="MegaCrit.Sts2.Core.Entities.Creatures.Creature.CombatId"/> damaged by <see cref="Cards.Spell.Todo.Continuos.Dark_Snake_Syndrome"/>.</summary>
public static class YgoDarkSnakeSyndromeTargetState
{
    private static readonly Dictionary<Player, uint> TargetEnemyCombatIdByPlayer = new();

    public static void Set(Player player, uint enemyCombatId) => TargetEnemyCombatIdByPlayer[player] = enemyCombatId;

    public static bool TryGet(Player player, out uint enemyCombatId) =>
        TargetEnemyCombatIdByPlayer.TryGetValue(player, out enemyCombatId);

    public static void Remove(Player player) => TargetEnemyCombatIdByPlayer.Remove(player);

    public static void ClearAll() => TargetEnemyCombatIdByPlayer.Clear();
}
