using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class GraveyardRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/graveyard.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/graveyard.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetGraveyardCountForOwner();

    private CardPile? _subscribedPile;

    private readonly HashSet<string> _annualKeysConsumedThisTurn = new();

    private int _dragonMonstersDestroyedThisTurn;

    private bool _sanctuaryHalveNextSpillToPlayer;

    /// <summary>While true, nested <see cref="DamageCmd.Attack"/> from splinter chain must not start another splinter chain.</summary>
    private bool _splinterChainRunning;

    /// <summary>
    /// Enemies (by <see cref="Creature.CombatId"/>) that have already triggered per-hit monster on-damage effects (Cestus, Bistro Butcher, Masked Sorcerer)
    /// for the current attack chain. Cleared at the start of each top-level <see cref="AfterAttack"/>; splinter nested calls share the same set so each enemy procs at most once per chain.
    /// </summary>
    private readonly HashSet<uint> _onDamageEffectSeenEnemyIds = new();

    public override Task BeforeCombatStart()
    {
        YgoCombatEndLifecycle.ResetDedupForNewCombat();
        SubscribeToGraveyardPile();
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await YgoCombatEndLifecycle.RunEndOfCombatCleanupIfNeededAsync(room);

        UnsubscribeFromGraveyardPile();
        _sanctuaryHalveNextSpillToPlayer = false;
        YgoPortionedSalvo.ClearAll();
    }

    /// <summary>At the start of your turn, set your star count to 1 (from the graveyard).</summary>
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner?.Creature.Side)
            await PlayerCmd.SetStars(1, Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        YgoPlayerCombatTurnStamp.Bump(player);
        YgoSanganNameLock.Clear(player);

        YgoDealWithDarkRulerState.OnPlayerTurnStart(player);

        if (player == Owner)
        {
            _annualKeysConsumedThisTurn.Clear();
            _dragonMonstersDestroyedThisTurn = 0;
            _sanctuaryHalveNextSpillToPlayer = false;
            YgoDarkSpiritSilentState.ClearForPlayer(player);
            FairyOfSpringReturnedEquipLock.ClearAll();
        }

        if (player != Owner || player.PlayerCombatState == null || player.Creature == null)
            return;

        await YgoJamBreedingMachineContinuous.TryResolvePlayerTurnStartForPhase(
            choiceContext,
            player,
            YgoOwnerTurnStartSpellTrapDispatchPhase.BeforeOwnerFieldPetHooks);

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard bm)
                await bm.OnGraveyardRelicOwnerTurnStartForFieldPetAsync(choiceContext, player, pet, this);
        }

        foreach (CardModel c in GetGraveyardCards(player))
        {
            if (c is BaseMonsterCard bm)
                await bm.OnGraveyardRelicOwnerTurnStartWhileInGraveyardAsync(choiceContext, player, this);
        }

        await YgoSealmasterMeiseiGate.DestroyTalismansIfNoSealmaster(player);
        await YgoJamBreedingMachineContinuous.TryResolvePlayerTurnStartForPhase(
            choiceContext,
            player,
            YgoOwnerTurnStartSpellTrapDispatchPhase.AfterSealmasterBeforeFieldMonsterHooks);
        await YgoOwnerTurnStartFieldMonsterHooks.TryResolvePlayerTurnStart(choiceContext, player);
    }

    /// <summary>Bottomless Shifting Sand: hand count for its effect uses size before the end-of-turn discard flush.</summary>
    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState != null)
        {
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (pet.GetPower<BazooSoulEaterTempAtkPower>() != null)
                    await PowerCmd.Remove<BazooSoulEaterTempAtkPower>(pet);
                if (pet.GetPower<SpiritRyuTempAtkDefPower>() != null)
                    await PowerCmd.Remove<SpiritRyuTempAtkDefPower>(pet);
            }
        }

        await YgoOwnerBeforeTurnEndFlushHooks.DispatchAsync(choiceContext, player);
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        await base.AfterCreatureAddedToCombat(creature);
        if (creature.Side != CombatSide.Enemy || !creature.IsAlive || creature.CombatState == null)
            return;

                var ctx = YgoChoiceContexts.Blocking();
        foreach (Player p in creature.CombatState.Players)
        {
            if (p.Creature?.Side != CombatSide.Player)
                continue;
            await SliferSkyDragonService.ApplyAllSliferPressureToEnemyAsync(ctx, p, creature);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player && Owner?.PlayerCombatState != null)
        {
            await MonsterCommandRegistry.ResolveOwnerTurnEndFieldCleanupAsync(choiceContext, Owner);
            MonsterCommandRegistry.ClearPerTurnExtrasForPlayer(Owner);
        }
    }

    /// <summary>Once-per-turn (annual) gate keyed by string; returns true the first call each player turn.</summary>
    public bool TryConsumeAnnual(string key)
    {
        if (_annualKeysConsumedThisTurn.Contains(key))
            return false;
        _annualKeysConsumedThisTurn.Add(key);
        return true;
    }

    public bool IsAnnualAvailable(string key) => !_annualKeysConsumedThisTurn.Contains(key);

    /// <summary>Chunk Z: Splinter (first 50% of past-block to each other enemy, then N−1 independent decay chains); on-hit Blight (50% of hit damage as stacks, including blocked); Shinato Corpse-Blight (execute kill: 50% of that damage as Blight to all enemies) after duel monster <see cref="AttackCommand"/>.</summary>
    public override async Task AfterAttack(AttackCommand command)
    {
        if (Owner == null || command.Attacker?.Player != Owner)
            return;

        if (!command.IsSingleTargeted)
            return;

        if (command.ModelSource is BaseMonsterCard monster)
        {
            await AfterAttack_FromDuelMonsterAsync(command, monster);
            return;
        }

        if (command.ModelSource is YgoDuelistCard ygo && ygo.CardDamagePortionCount >= 2)
            await AfterAttack_FromPortionedYgoCardAsync(command, ygo);
    }

    private async Task AfterAttack_FromDuelMonsterAsync(AttackCommand command, BaseMonsterCard monster)
    {
        var ctx = YgoChoiceContexts.Blocking();
        CombatState? cs = command.Attacker.CombatState;
        if (cs == null)
            return;

        Player? atkOwner = command.Attacker.Player;
        Player? atkPlayer = atkOwner;

        bool portionSalvo = monster.AttackPortionCount >= 2 && YgoPortionedSalvo.IsMonsterSalvoFor(monster);
        bool skipClearAndOpening = portionSalvo && YgoPortionedSalvo.IsMidMonsterSalvoPastFirstChunk(monster);

        if (!_splinterChainRunning && !skipClearAndOpening)
        {
            _onDamageEffectSeenEnemyIds.Clear();

            await monster.OnGraveyardRelicAfterAttackOpeningAsync(command, atkOwner, ctx);

            // Main hit before splinter so "first damage" order matches combat; shared set dedupes splinter bounces per enemy per chain.
            await ProcessMonsterUnblockedOnDamageEffectsAsync(command, monster, atkPlayer, ctx);
        }
        else if (!_splinterChainRunning)
            await ProcessMonsterUnblockedOnDamageEffectsAsync(command, monster, atkPlayer, ctx);

        bool morePortionRemain = false;
        if (portionSalvo && !_splinterChainRunning)
            morePortionRemain = YgoPortionedSalvo.RecordMonsterChunkAndReturnIfMoreRemain(monster, command.Results);

        bool consumedPortionFinal = false;
        int aggregatedPastBlockOnPrimary = 0;
        Dictionary<uint, int> aggregatedBlightByEnemyId = new();
        Creature? portionPrimaryReceiver = null;
        if (portionSalvo && !_splinterChainRunning && !morePortionRemain)
        {
            consumedPortionFinal = YgoPortionedSalvo.TryConsumeMonsterSalvoFinal(
                monster,
                out aggregatedPastBlockOnPrimary,
                out aggregatedBlightByEnemyId,
                out portionPrimaryReceiver);
        }

        bool splinter = monster.AttackDealsSplinterDamage;
        if (!splinter)
            splinter = EnragedBattleOxService.AttackGetsSplinterFromOxAura(monster, atkOwner);
        if (!splinter)
        {
            foreach (var eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
            {
                if (eq is BaseEquipSpellCard be && be.GrantsSplinterTo(monster))
                {
                    splinter = true;
                    break;
                }
            }
        }

        if (splinter && !_splinterChainRunning)
        {
            if (!morePortionRemain)
            {
                _splinterChainRunning = true;
                try
                {
                    if (consumedPortionFinal && portionPrimaryReceiver != null && aggregatedPastBlockOnPrimary > 0)
                    {
                        await ResolveSplinterChainAsync(
                            ctx,
                            command,
                            monster,
                            cs,
                            aggregatedPastBlockOnPrimary,
                            portionPrimaryReceiver);
                    }
                    else if (!portionSalvo)
                        await ResolveSplinterChainAsync(ctx, command, monster, cs);
                    else if (!consumedPortionFinal)
                        await ResolveSplinterChainAsync(ctx, command, monster, cs);
                }
                finally
                {
                    _splinterChainRunning = false;
                }
            }
        }

        bool blighted = monster.AttackDealsBlightedDamage;
        if (!blighted)
        {
            foreach (var eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
            {
                if (eq is BaseEquipSpellCard be && be.GrantsBlightTo(monster))
                {
                    blighted = true;
                    break;
                }
            }
        }

        if (!blighted && command.Attacker?.GetPower<SecretPassTreasuresBlightPower>() != null)
            blighted = true;

        if (blighted && !morePortionRemain)
        {
            decimal blightMultiplier = 0.5m;
            if (command.Attacker?.GetPower<SecretPassTreasuresBlightPower>() is { Amount: var secretPassPct })
                blightMultiplier = secretPassPct / 100m;
            else if (monster.AttackDealsFullBlightedDamage)
                blightMultiplier = 1m;

            if (consumedPortionFinal)
            {
                foreach (Creature victim in cs.GetOpponentsOf(command.Attacker))
                {
                    if (!victim.IsAlive || victim.CombatId is not uint cid)
                        continue;
                    if (!aggregatedBlightByEnemyId.TryGetValue(cid, out int hitDamage) || hitDamage <= 0)
                        continue;

                    int blight = (int)decimal.Floor(hitDamage * blightMultiplier);
                    if (blight <= 0)
                        continue;

                    await PowerCmd.Apply<BlightPower>(victim, blight, command.Attacker, monster);
                }
            }
            else
            {
                foreach (DamageResult r in command.Results)
                {
                    int hitDamage = YgoExecuteKillShared.FullIncomingDamage(r);
                    if (r.Receiver.Side != CombatSide.Enemy || hitDamage <= 0)
                        continue;

                    int blight = (int)decimal.Floor(hitDamage * blightMultiplier);
                    if (blight <= 0)
                        continue;

                    await PowerCmd.Apply<BlightPower>(r.Receiver, blight, command.Attacker, monster);
                }
            }
        }

        // Splinter follow-up hits run nested AfterAttack while _splinterChainRunning; execute-style hooks are skipped there and applied in ResolveSplinterChainAsync via ProcessMonsterExecuteKillEffectsAsync so splinter kills count as that monster's execute.
        if (!_splinterChainRunning)
            await ProcessMonsterExecuteKillEffectsAsync(command, monster, cs);

        if (!_splinterChainRunning && monster is D_D_Warrior or D_D_Warrior_Lady && monster is NormalMonsterCard nmc)
        {
            Creature? ddPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(nmc);
            if (ddPet != null)
                MonsterCommandRegistry.GetOrCreate(ddPet).HasAttackedThisTurn = true;
        }

        // Splinter nested attacks: on-damage heal/draw once per unique enemy per chain (see _onDamageEffectSeenEnemyIds).
        if (_splinterChainRunning)
            await ProcessMonsterUnblockedOnDamageEffectsAsync(command, monster, atkPlayer, ctx);
    }

    private async Task AfterAttack_FromPortionedYgoCardAsync(AttackCommand command, YgoDuelistCard card)
    {
        CombatState? cs = command.Attacker.CombatState;
        if (cs == null)
            return;

        bool portionSalvo = YgoPortionedSalvo.IsCardSalvoFor(card);
        bool morePortionRemain = false;
        if (portionSalvo)
            morePortionRemain = YgoPortionedSalvo.RecordCardChunkAndReturnIfMoreRemain(card, command.Results);

        bool consumedFinal = false;
        Dictionary<uint, int> aggBlight = new();
        if (portionSalvo && !morePortionRemain)
            consumedFinal = YgoPortionedSalvo.TryConsumeCardSalvoFinal(card, out _, out aggBlight, out _);

        bool blighted = card.CardShowsBlightKeyword;
        if (!blighted && command.Attacker?.GetPower<SecretPassTreasuresBlightPower>() != null)
            blighted = true;

        if (blighted && !morePortionRemain)
        {
            decimal blightMultiplier = 0.5m;
            if (command.Attacker?.GetPower<SecretPassTreasuresBlightPower>() is { Amount: var secretPassPct })
                blightMultiplier = secretPassPct / 100m;

            if (consumedFinal)
            {
                foreach (Creature victim in cs.GetOpponentsOf(command.Attacker))
                {
                    if (!victim.IsAlive || victim.CombatId is not uint cid)
                        continue;
                    if (!aggBlight.TryGetValue(cid, out int hitDamage) || hitDamage <= 0)
                        continue;

                    int blight = (int)decimal.Floor(hitDamage * blightMultiplier);
                    if (blight <= 0)
                        continue;

                    await PowerCmd.Apply<BlightPower>(victim, blight, command.Attacker, card);
                }
            }
            else
            {
                foreach (DamageResult r in command.Results)
                {
                    int hitDamage = YgoExecuteKillShared.FullIncomingDamage(r);
                    if (r.Receiver.Side != CombatSide.Enemy || hitDamage <= 0)
                        continue;

                    int blight = (int)decimal.Floor(hitDamage * blightMultiplier);
                    if (blight <= 0)
                        continue;

                    await PowerCmd.Apply<BlightPower>(r.Receiver, blight, command.Attacker, card);
                }
            }
        }
    }

    /// <summary>
    /// Cestus heal, Bistro Butcher draw, Masked Sorcerer draw — only for enemies taking unblocked damage for the first time in this attack chain (main + splinter).
    /// </summary>
    private async Task ProcessMonsterUnblockedOnDamageEffectsAsync(
        AttackCommand command,
        BaseMonsterCard monster,
        Player? atkPlayer,
        BlockingPlayerChoiceContext ctx)
    {
        if (atkPlayer?.Creature == null)
            return;

        foreach (DamageResult r in command.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || r.UnblockedDamage <= 0)
                continue;
            if (r.Receiver.CombatId is not uint cid)
                continue;
            if (!_onDamageEffectSeenEnemyIds.Add(cid))
                continue;

            foreach (BaseEquipSpellCard eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
            {
                if (eq is IEquipFirstUnblockedDamageEffect fx)
                {
                    await fx.ApplyWhenEquippedMonsterDealsFirstUnblockedDamageAsync(ctx, atkPlayer, monster, r);
                    break;
                }
            }

            await monster.OnFirstUnblockedDamageToEnemyThisChainAsync(command, r, atkPlayer, ctx);
        }
    }

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner.Creature || !_sanctuaryHalveNextSpillToPlayer)
            return amount;

        _sanctuaryHalveNextSpillToPlayer = false;
        if (amount <= 0m)
            return amount;
        return amount / 2m;
    }

    public override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
    {
        if (Owner == null || attack.Attacker == null || attack.Attacker.Side != CombatSide.Enemy)
            return hitCount;
        if (attack.TargetSide != CombatSide.Player)
            return hitCount;
        if (!YgoDarkSpiritSilentState.ShouldDoubleAttack(Owner, attack.Attacker))
            return hitCount;
        return hitCount * 2;
    }

    /// <summary>
    /// Permanent execute ATK (<see cref="BaseMonsterCard.PermanentAtkDeltaOnEnemyExecute"/>), then per-card <see cref="BaseMonsterCard.OnEnemyExecutedByThisAttackAsync"/>.
    /// Called for the main hit from <see cref="AfterAttack"/> and for each splinter hit from <see cref="ResolveSplinterChainAsync"/> (nested AfterAttack skips while <see cref="_splinterChainRunning"/> to avoid double-processing).
    /// </summary>
    private static async Task ProcessMonsterExecuteKillEffectsAsync(
        AttackCommand command,
        BaseMonsterCard monster,
        CombatState cs)
    {
        int killBonus = monster.PermanentAtkDeltaOnEnemyExecute;
        if (killBonus != 0)
        {
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                    continue;
                if (!monster.AppliesPermanentAtkDeltaOnEnemyKill(r.Receiver))
                    continue;
                monster.ApplyPermanentExecuteAtkDelta(killBonus);
            }
        }

        await monster.OnEnemyExecutedByThisAttackAsync(command, cs);
    }

    /// <summary>
    /// First splinter = half of damage past block on the struck enemy. That amount fans out to <b>each</b> other living enemy (N−1 branches);
    /// each branch then chains independently with min(floor(prev base / 2), floor(past block on last hit / 2)) and round-robin picks for later hops.
    /// </summary>
    private static async Task ResolveSplinterChainAsync(
        BlockingPlayerChoiceContext ctx,
        AttackCommand command,
        BaseMonsterCard monster,
        CombatState cs,
        int? aggregatedPastBlockOnPrimary = null,
        Creature? aggregatedMainReceiver = null)
    {
        Creature attacker = command.Attacker;
        if (aggregatedPastBlockOnPrimary is int pbAgg
            && aggregatedMainReceiver != null
            && aggregatedMainReceiver.Side == CombatSide.Enemy
            && pbAgg > 0)
        {
            int firstBase = (int)decimal.Floor(pbAgg * 0.5m);
            if (firstBase > 0)
            {
                Creature mainReceiver = aggregatedMainReceiver;
                List<Creature> initialOthers = cs.GetOpponentsOf(attacker)
                    .Where(c => c.IsAlive && !ReferenceEquals(c, mainReceiver))
                    .ToList();

                foreach (Creature initialOther in initialOthers)
                    await ResolveSplinterBranchAsync(ctx, attacker, monster, cs, firstBase, initialOther);
            }

            return;
        }

        foreach (DamageResult r in command.Results)
        {
            int pastBlock = DamagePastBlock(r);
            if (r.Receiver.Side != CombatSide.Enemy || pastBlock <= 0)
                continue;

            int firstBase = (int)decimal.Floor(pastBlock * 0.5m);
            if (firstBase <= 0)
                continue;

            Creature mainReceiver = r.Receiver;
            List<Creature> initialOthers = cs.GetOpponentsOf(attacker)
                .Where(c => c.IsAlive && !ReferenceEquals(c, mainReceiver))
                .ToList();

            foreach (Creature initialOther in initialOthers)
                await ResolveSplinterBranchAsync(ctx, attacker, monster, cs, firstBase, initialOther);
        }
    }

    /// <summary>One splinter chain: first hit goes to <paramref name="firstTarget"/>; subsequent hops use <see cref="PickNextSplinterVictim"/>.</summary>
    private static async Task ResolveSplinterBranchAsync(
        BlockingPlayerChoiceContext ctx,
        Creature attacker,
        BaseMonsterCard monster,
        CombatState cs,
        int baseAmount,
        Creature firstTarget)
    {
        Creature? next = firstTarget;
        while (baseAmount > 0 && next != null)
        {
            if (!next.IsAlive)
                break;

            AttackCommand splinterCmd = await DamageCmd.Attack(baseAmount)
                .FromCard(monster)
                .Targeting(next)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(ctx);

            await ProcessMonsterExecuteKillEffectsAsync(splinterCmd, monster, cs);

            int pastBlockOnVictim = 0;
            foreach (DamageResult dr in splinterCmd.Results)
            {
                if (dr.Receiver == next)
                    pastBlockOnVictim += DamagePastBlock(dr);
            }

            int chainCeiling = baseAmount / 2;
            int damageCandidate = (int)decimal.Floor(pastBlockOnVictim * 0.5m);
            Creature lastHit = next;
            baseAmount = Math.Min(chainCeiling, damageCandidate);
            next = PickNextSplinterVictim(attacker, lastHit, cs);
        }
    }

    /// <summary>
    /// Next splinter target among living opponents. When the struck enemy dies, <paramref name="lastHit"/> may be dead and not in the ring;
    /// we still splinter to the sole survivor when two enemies started and one was killed by this hit.
    /// </summary>
    private static Creature? PickNextSplinterVictim(Creature attacker, Creature lastHit, CombatState cs)
    {
        List<Creature> ring = cs.GetOpponentsOf(attacker).Where(c => c.IsAlive).ToList();
        if (ring.Count == 0)
            return null;

        if (ring.Count == 1)
        {
            Creature only = ring[0];
            return ReferenceEquals(only, lastHit) ? null : only;
        }

        int idx = ring.FindIndex(c => ReferenceEquals(c, lastHit));
        if (idx < 0)
            return ring[0];

        for (int step = 1; step <= ring.Count; step++)
        {
            Creature c = ring[(idx + step) % ring.Count];
            if (c.IsAlive && !ReferenceEquals(c, lastHit))
                return c;
        }

        return null;
    }

    /// <summary>Damage that got past block: HP removed + overkill (not half of <see cref="DamageResult.BlockedDamage"/>).</summary>
    private static int DamagePastBlock(DamageResult r) => r.UnblockedDamage + r.OverkillDamage;

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || Owner.Creature != target || dealer == null || dealer.Side != CombatSide.Enemy)
            return amount;
        if (!props.HasFlag(ValueProp.Move))
            return amount;
        if (!YgoDarkSpiritSilentState.IsNegated(Owner, dealer))
            return amount;
        return 0m;
    }

    /// <summary>Gets the graveyard pile for the current combat player, or null if not in combat.</summary>
    public static CardPile? GetGraveyardPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
    }

    /// <summary>Graveyard card count for this relic's owner (for relic display).</summary>
    private int GetGraveyardCountForOwner()
    {
        var player = Owner;
        if (player == null)
            return 0;

        var pile = GetGraveyardPile(player);
        return pile?.Cards.Count ?? 0;
    }

    private void SubscribeToGraveyardPile()
    {
        var player = Owner;
        if (player == null)
            return;

        var pile = GetGraveyardPile(player);
        if (pile == null)
            return;

        // Avoid double-subscription.
        if (_subscribedPile != null)
            UnsubscribeFromGraveyardPile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnGraveyardContentsChanged;
        // Force initial refresh.
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromGraveyardPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnGraveyardContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnGraveyardContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    /// <summary>Cards in the graveyard pile for the given player (for relic click view).</summary>
    public static IReadOnlyList<CardModel> GetGraveyardCards(Player? player)
    {
        var pile = GetGraveyardPile(player);
        if (pile == null) return [];
        return pile.Cards.ToList();
    }

    public static bool IsGraveyardRelic(RelicModel? model) => model is GraveyardRelic;

    public static GraveyardRelic? AsGraveyard(RelicModel? model) => model as GraveyardRelic;

    /// <summary>
    /// When a Fairy duel monster with Die for You dies under The Sanctuary in the Sky, the next spill damage
    /// to the player from that same hit is halved (see <see cref="ModifyHpLostAfterOsty"/>).
    /// </summary>
    public static void ArmSanctuaryHalveNextSpillDamage(Player? player)
    {
        GraveyardRelic? g = YgoPlayerRelicAccess.GetRelic<GraveyardRelic>(player);
        if (g != null)
            g._sanctuaryHalveNextSpillToPlayer = true;
    }

    public static void RegisterDragonMonsterDestroyed(Player? player)
    {
        GraveyardRelic? g = YgoPlayerRelicAccess.GetRelic<GraveyardRelic>(player);
        g?.NotifyDragonMonsterDestroyed();
    }

    private void NotifyDragonMonsterDestroyed() => _dragonMonstersDestroyedThisTurn++;

    /// <summary>Dragon duel monsters destroyed during the current player turn (for Super Rejuvenation). Cleared at the start of your next turn.</summary>
    public int DragonMonstersDestroyedThisTurn => _dragonMonstersDestroyedThisTurn;
}
