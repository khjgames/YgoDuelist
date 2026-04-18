using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Deterministic RNG for YGO-style dice / coin-flip / "random" effects that must not consume RunState RNG streams.
/// Mixes <see cref="MegaCrit.Sts2.Core.Runs.RunRngSet.Seed"/> (run string seed hash), <see cref="MegaCrit.Sts2.Core.Runs.RunState.TotalFloor"/>,
/// <see cref="CombatState.RoundNumber"/>, <see cref="CombatState.CurrentSide"/>, caller <paramref name="salt"/>,
/// and <paramref name="mix"/> (use <see cref="MixSpellTrapZoneSlot"/> / <see cref="MixDuelMonsterAttack"/> / <see cref="MixNetCombatCard"/> for MP-stable identity).
/// </summary>
public static class YgoDeterministicRng
{
    /// <summary>
    /// Spell/trap field cards: uses <see cref="NetCombatCard"/> index (replicated) instead of zone list index (can differ on observers).
    /// </summary>
    public static ulong MixSpellTrapZoneSlot(Player? player, CardModel? card)
    {
        if (player?.Creature == null || card == null)
            return 0uL;

        unchecked
        {
            ulong idx = NetCombatCardIndexOrFallback(card);
            uint cid = player.Creature.CombatId ?? 0u;
            ulong mix = idx ^ ((ulong)cid << 32);
            mix ^= player.NetId;
            return mix;
        }
    }

    /// <summary>Coin/dice tied to a duel monster attack: <see cref="CardPlay"/> identity is MP-stable (no <c>RuntimeHelpers.GetHashCode</c>).</summary>
    public static ulong MixDuelMonsterAttack(Creature? playerCreature, Creature? pet, CardPlay cardPlay)
    {
        ulong mix = MixCardPlay(cardPlay);
        if (playerCreature != null)
            mix ^= (ulong)(playerCreature.CombatId ?? 0u) << 32;
        if (pet != null)
            mix ^= (ulong)(pet.CombatId ?? 0u);
        return mix;
    }

    /// <summary>Generic per-card mix for effects that only have a <see cref="CardModel"/> (e.g. option-pile display helpers).</summary>
    public static ulong MixNetCombatCard(CardModel card) => NetCombatCardIndexOrFallback(card);

    /// <inheritdoc cref="MixDuelMonsterAttack"/>
    public static ulong MixCardPlay(CardPlay cardPlay)
    {
        unchecked
        {
            ulong mix = NetCombatCardIndexOrFallback(cardPlay.Card);
            mix ^= (ulong)(uint)cardPlay.PlayIndex;
            mix ^= (ulong)(uint)cardPlay.PlayCount << 17;
            uint tid = cardPlay.Target?.CombatId ?? 0u;
            mix ^= (ulong)tid << 40;
            return mix;
        }
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

    private static ulong NetCombatCardIndexOrFallback(CardModel card)
    {
        try
        {
            return NetCombatCard.FromModel(card).CombatCardIndex;
        }
        catch (Exception ex)
        {
            YgoMpDiagnostics.VerbosePrint(
                "DetRng",
                $"NetCombatCard.FromModel fallback id={card.Id?.Entry}: {ex.Message}");
            return Fnv1a(card.Id?.Entry ?? "");
        }
    }

    private static int NextInt(CombatState combatState, string salt, int minInclusive, int maxExclusive, ulong mix)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be > minInclusive.");

        unchecked
        {
            uint seed = 2166136261u;
            seed = Fnv1a(seed, combatState.RunState.Rng.Seed);
            seed = Fnv1a(seed, (uint)combatState.RunState.TotalFloor);
            seed = Fnv1a(seed, (uint)combatState.RoundNumber);
            seed = Fnv1a(seed, (uint)(int)combatState.CurrentSide);
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
