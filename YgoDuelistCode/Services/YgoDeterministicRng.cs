using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Deterministic RNG for YGO-style dice / coin-flip / "random" effects that must not consume RunState RNG.
/// Seed mixes run floor, combat round number, caller <paramref name="salt"/>,
/// and optional <paramref name="mix"/> (use <see cref="MixSpellTrapZoneSlot"/> for trap cards so multiple copies differ in multiplayer).
/// </summary>
public static class YgoDeterministicRng
{
    /// <summary>Stable per combatant + spell/trap zone index for isolating coin/dice outcomes between duplicate field cards.</summary>
    public static ulong MixSpellTrapZoneSlot(Player? player, CardModel? card)
    {
        if (player?.Creature == null || card == null)
            return 0uL;

        CardPile? pile = SpellTrapZonePile.CustomType.GetPile(player);
        int idx = -1;
        if (pile != null)
        {
            for (int i = 0; i < pile.Cards.Count; i++)
            {
                if (ReferenceEquals(pile.Cards[i], card))
                {
                    idx = i;
                    break;
                }
            }
        }

        if (idx < 0)
            idx = 0;

        unchecked
        {
            Creature c = player.Creature;
            uint cid = c.CombatId ?? 0u;
            return ((ulong)cid << 32) | (uint)idx;
        }
    }

    /// <summary>Isolates coin outcomes per attack play and duel monster pet (Jirai Gumo).</summary>
    public static ulong MixDuelMonsterAttack(Creature? playerCreature, Creature? pet, CardPlay cardPlay)
    {
        ulong mix = (ulong)(uint)RuntimeHelpers.GetHashCode(cardPlay);
        if (playerCreature != null)
            mix ^= (ulong)(playerCreature.CombatId ?? 0u) << 32;
        if (pet != null)
            mix ^= (ulong)(pet.CombatId ?? 0u);
        return mix;
    }

    public static int RollDie(CombatState combatState, int sides, string salt, ulong mix = 0)
    {
        if (combatState == null)
            throw new ArgumentNullException(nameof(combatState));
        if (sides <= 0)
            throw new ArgumentOutOfRangeException(nameof(sides), "Sides must be > 0.");
        return NextInt(combatState, salt, 1, sides + 1, mix);
    }

    public static bool CoinFlip(CombatState combatState, string salt, ulong mix = 0) =>
        NextInt(combatState, salt, 0, 2, mix) == 1;

    public static T? PickOne<T>(CombatState combatState, IReadOnlyList<T> items, string salt, ulong mix = 0)
    {
        if (combatState == null)
            throw new ArgumentNullException(nameof(combatState));
        if (items == null)
            throw new ArgumentNullException(nameof(items));
        if (items.Count == 0)
            return default;

        int idx = NextInt(combatState, salt, 0, items.Count, mix);
        return items[idx];
    }

    public static IReadOnlyList<T> StableOrder<T>(IEnumerable<T> items, Func<T, uint?> stableKey) =>
        items.OrderBy(x => stableKey(x) ?? uint.MaxValue).ToList();

    private static int NextInt(CombatState combatState, string salt, int minInclusive, int maxExclusive, ulong mix)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be > minInclusive.");

        unchecked
        {
            uint seed = 2166136261u;
            seed = Fnv1a(seed, (uint)combatState.RunState.TotalFloor);
            seed = Fnv1a(seed, (uint)combatState.RoundNumber);
            seed = Fnv1a(seed, Fnv1a(salt));
            seed = Fnv1a(seed, (uint)(mix & 0xFFFFFFFFu));
            seed = Fnv1a(seed, (uint)(mix >> 32));

            uint x = seed == 0 ? 0x6D2B79F5u : seed;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;

            uint range = (uint)(maxExclusive - minInclusive);
            int val = (int)(x % range);
            return minInclusive + val;
        }
    }

    private static uint Fnv1a(uint hash, uint data)
    {
        hash ^= (data & 0xFF);
        hash *= 16777619u;
        hash ^= ((data >> 8) & 0xFF);
        hash *= 16777619u;
        hash ^= ((data >> 16) & 0xFF);
        hash *= 16777619u;
        hash ^= ((data >> 24) & 0xFF);
        hash *= 16777619u;
        return hash;
    }

    private static uint Fnv1a(string s)
    {
        unchecked
        {
            uint hash = 2166136261u;
            if (string.IsNullOrEmpty(s))
                return hash;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
