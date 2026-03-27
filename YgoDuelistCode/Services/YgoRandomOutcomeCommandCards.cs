using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Builds random coin / d6 <see cref="MonsterCommandCard"/> instances for the option pile (display-only).
/// </summary>
public static class YgoRandomOutcomeCommandCards
{
    /// <summary>50/50 Heads vs Tails, then <see cref="MonsterCommandCard.InitializeSource"/>.</summary>
    public static MonsterCommandCard CreateRandomCoinFlipDisplay(Player player, NormalMonsterCard source)
    {
        var combatState = player.Creature?.CombatState;
        if (combatState == null)
            throw new System.InvalidOperationException("Creature.CombatState required.");

        bool heads = GD.Randf() < 0.5f;
        MonsterCommandCard card = heads
            ? combatState.CreateCard<Heads>(player)
            : combatState.CreateCard<Tails>(player);
        card.InitializeSource(source);
        return card;
    }

    /// <summary>Uniform 1–6, then <see cref="MonsterCommandCard.InitializeSource"/>.</summary>
    public static MonsterCommandCard CreateRandomD6RollDisplay(Player player, NormalMonsterCard source)
    {
        var combatState = player.Creature?.CombatState;
        if (combatState == null)
            throw new System.InvalidOperationException("Creature.CombatState required.");

        int face = GD.RandRange(1, 6);
        MonsterCommandCard card = face switch
        {
            1 => combatState.CreateCard<Rolled_1>(player),
            2 => combatState.CreateCard<Rolled_2>(player),
            3 => combatState.CreateCard<Rolled_3>(player),
            4 => combatState.CreateCard<Rolled_4>(player),
            5 => combatState.CreateCard<Rolled_5>(player),
            _ => combatState.CreateCard<Rolled_6>(player),
        };
        card.InitializeSource(source);
        return card;
    }
}
