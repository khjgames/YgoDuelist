using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tracks multi-chunk Portion salvos so <see cref="Relics.GraveyardRelic"/> can aggregate Blight and Splinter once per logical attack.
/// </summary>
public static class YgoPortionedSalvo
{
    private static readonly object Gate = new();

    private static readonly Dictionary<BaseMonsterCard, MonsterSalvoState> MonsterStates = new();
    private static readonly Dictionary<YgoDuelistCard, CardSalvoState> CardStates = new();

    private static int DamagePastBlock(DamageResult r) => r.UnblockedDamage + r.OverkillDamage;

    public static bool IsMonsterSalvoFor(BaseMonsterCard monster)
    {
        lock (Gate)
            return MonsterStates.ContainsKey(monster);
    }

    /// <summary>True starting from the second completed chunk (opening hooks run only on the first chunk).</summary>
    public static bool IsMidMonsterSalvoPastFirstChunk(BaseMonsterCard monster)
    {
        lock (Gate)
        {
            if (!MonsterStates.TryGetValue(monster, out MonsterSalvoState? s))
                return false;
            return s.ChunksRecorded >= 1;
        }
    }

    /// <summary>
    /// <see cref="NormalMonsterCard.CombatAction"/> calls <see cref="YgoPortionDamage.DealMonsterAttackToTargetAsync"/> once per target;
    /// card hooks run once after the full salvo returns, so this is always false at that callsite.
    /// </summary>
    public static bool ShouldSkipMonsterPerHitCardHook(BaseMonsterCard _) => false;

    public static bool IsCardSalvoFor(YgoDuelistCard card)
    {
        lock (Gate)
            return CardStates.ContainsKey(card);
    }

    /// <summary>Clears any in-flight salvo state (e.g. combat end) so a stuck salvo cannot taint the next fight.</summary>
    public static void ClearAll()
    {
        lock (Gate)
        {
            MonsterStates.Clear();
            CardStates.Clear();
        }
    }

    public static void BeginMonsterSalvo(BaseMonsterCard monster, Creature primaryTarget, int[] portions)
    {
        lock (Gate)
        {
            MonsterStates[monster] = new MonsterSalvoState(primaryTarget, portions);
        }
    }

    public static void BeginCardSalvo(YgoDuelistCard card, Creature? primaryTarget, int[] portions)
    {
        lock (Gate)
        {
            CardStates[card] = new CardSalvoState(primaryTarget, portions);
        }
    }

    /// <summary>Returns true when further chunk <see cref="AttackCommand"/> executions are still expected after this one.</summary>
    public static bool RecordMonsterChunkAndReturnIfMoreRemain(
        BaseMonsterCard monster,
        IEnumerable<DamageResult> results)
    {
        lock (Gate)
        {
            if (!MonsterStates.TryGetValue(monster, out MonsterSalvoState? s))
                return false;
            s.RecordChunkResults(results);
            return s.ChunksRecorded < s.Portions.Length;
        }
    }

    public static bool RecordCardChunkAndReturnIfMoreRemain(YgoDuelistCard card, IEnumerable<DamageResult> results)
    {
        lock (Gate)
        {
            if (!CardStates.TryGetValue(card, out CardSalvoState? s))
                return false;
            s.RecordChunkResults(results);
            return s.ChunksRecorded < s.Portions.Length;
        }
    }

    public static bool TryConsumeMonsterSalvoFinal(
        BaseMonsterCard monster,
        out int aggregatedPastBlockOnPrimary,
        out Dictionary<uint, int> aggregatedBlightByEnemyId,
        out Creature? portionPrimaryReceiver)
    {
        lock (Gate)
        {
            aggregatedPastBlockOnPrimary = 0;
            aggregatedBlightByEnemyId = new Dictionary<uint, int>();
            portionPrimaryReceiver = null;
            if (!MonsterStates.TryGetValue(monster, out MonsterSalvoState? s))
                return false;

            aggregatedPastBlockOnPrimary = s.AggregatedPastBlockOnPrimary;
            foreach (KeyValuePair<uint, int> kv in s.BlightByEnemyId)
                aggregatedBlightByEnemyId[kv.Key] = kv.Value;
            portionPrimaryReceiver = s.PrimaryTarget;
            MonsterStates.Remove(monster);
            return true;
        }
    }

    public static bool TryConsumeCardSalvoFinal(
        YgoDuelistCard card,
        out int aggregatedPastBlockOnPrimary,
        out Dictionary<uint, int> aggregatedBlightByEnemyId,
        out Creature? portionPrimaryReceiver)
    {
        lock (Gate)
        {
            aggregatedPastBlockOnPrimary = 0;
            aggregatedBlightByEnemyId = new Dictionary<uint, int>();
            portionPrimaryReceiver = null;
            if (!CardStates.TryGetValue(card, out CardSalvoState? s))
                return false;

            aggregatedPastBlockOnPrimary = s.AggregatedPastBlockOnPrimary;
            foreach (KeyValuePair<uint, int> kv in s.BlightByEnemyId)
                aggregatedBlightByEnemyId[kv.Key] = kv.Value;
            portionPrimaryReceiver = s.PrimaryTarget;
            CardStates.Remove(card);
            return true;
        }
    }

    private sealed class MonsterSalvoState
    {
        internal MonsterSalvoState(Creature primaryTarget, int[] portions)
        {
            PrimaryTarget = primaryTarget;
            Portions = portions;
        }

        internal Creature PrimaryTarget { get; }
        internal int[] Portions { get; }
        internal int ChunksRecorded { get; private set; }
        internal int AggregatedPastBlockOnPrimary { get; private set; }
        internal Dictionary<uint, int> BlightByEnemyId { get; } = new();

        internal void RecordChunkResults(IEnumerable<DamageResult> results)
        {
            foreach (DamageResult r in results)
            {
                if (r.Receiver.CombatId is uint cid)
                {
                    int full = YgoExecuteKillShared.FullIncomingDamage(r);
                    if (full > 0)
                        BlightByEnemyId[cid] = BlightByEnemyId.GetValueOrDefault(cid) + full;
                }

                if (ReferenceEquals(r.Receiver, PrimaryTarget) && r.Receiver.Side == CombatSide.Enemy)
                    AggregatedPastBlockOnPrimary += DamagePastBlock(r);
            }

            ChunksRecorded++;
        }
    }

    private sealed class CardSalvoState
    {
        internal CardSalvoState(Creature? primaryTarget, int[] portions)
        {
            PrimaryTarget = primaryTarget;
            Portions = portions;
        }

        internal Creature? PrimaryTarget { get; }
        internal int[] Portions { get; }
        internal int ChunksRecorded { get; private set; }
        internal int AggregatedPastBlockOnPrimary { get; private set; }
        internal Dictionary<uint, int> BlightByEnemyId { get; } = new();

        internal void RecordChunkResults(IEnumerable<DamageResult> results)
        {
            foreach (DamageResult r in results)
            {
                if (r.Receiver.CombatId is uint cid)
                {
                    int full = YgoExecuteKillShared.FullIncomingDamage(r);
                    if (full > 0)
                        BlightByEnemyId[cid] = BlightByEnemyId.GetValueOrDefault(cid) + full;
                }

                if (PrimaryTarget != null
                    && ReferenceEquals(r.Receiver, PrimaryTarget)
                    && r.Receiver.Side == CombatSide.Enemy)
                {
                    AggregatedPastBlockOnPrimary += DamagePastBlock(r);
                }
            }

            ChunksRecorded++;
        }
    }
}
