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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
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

    public override Task BeforeCombatStart()
    {
        SubscribeToGraveyardPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromGraveyardPile();
        _sanctuaryHalveNextSpillToPlayer = false;
        return Task.CompletedTask;
    }

    /// <summary>At the start of your turn, set your star count to 1 (from the graveyard).</summary>
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner?.Creature.Side)
            await PlayerCmd.SetStars(1, Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
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

        if (YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<The_Sanctuary_in_the_Sky>(player)
            && TryConsumeAnnual("SANCTUARY_MERCURY_DRAW"))
        {
            bool hasMercury = false;
            foreach (Creature pet in player.PlayerCombatState.Pets)
            {
                if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                    continue;
                if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is The_Agent_of_Wisdom_Mercury)
                {
                    hasMercury = true;
                    break;
                }
            }

            if (hasMercury)
                await CardPileCmd.Draw(choiceContext, 1, player);
        }

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            CardModel? src = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (src is not Cure_Mermaid mermaid)
                continue;
            string key = $"CURE_MERMAID_{pet.CombatId}";
            if (!TryConsumeAnnual(key))
                continue;
            decimal maintenance = mermaid.IsUpgraded ? 0m : 1m;
            if (maintenance > 0)
                await CreatureCmd.Damage(
                    choiceContext,
                    pet,
                    maintenance,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    player.Creature,
                    mermaid);
            await CreatureCmd.Heal(player.Creature, 1m);
        }

        if (GetGraveyardCards(player).Any(c => c is Darklord_Marie) && TryConsumeAnnual("DARKLORD_MARIE_GY"))
            await CreatureCmd.Heal(player.Creature, 1m);

        await YgoSealmasterMeiseiGate.DestroyTalismansIfNoSealmaster(player);
        await YgoBlindDestructionContinuous.TryResolvePlayerTurnStart(choiceContext, player);
        await YgoJamBreedingMachineContinuous.TryResolvePlayerTurnStart(choiceContext, player);
        await YgoCardTraderContinuous.TryResolvePlayerTurnStart(choiceContext, player);
        await SliferSkyDragonService.ApplySliferPressureToAllEnemiesAsync(choiceContext, player);
    }

    /// <summary>Bottomless Shifting Sand: hand count for its effect uses size before the end-of-turn discard flush.</summary>
    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            await YgoBottomlessShiftingSandContinuous.TryResolveAfterPlayerTurnEnd(choiceContext, Owner);
        await YgoMirageTokenEndPhase.TryResolveBeforePlayerTurnEndFlushAsync(choiceContext, player);
        await YgoInsectQueenEndPhase.TryResolveBeforePlayerTurnEndFlushAsync(choiceContext, player);
        await SliferSkyDragonService.BeforePlayerTurnEndFlushAsync(choiceContext, player);
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        await base.AfterCreatureAddedToCombat(creature);
        if (creature.Side != CombatSide.Enemy || !creature.IsAlive || creature.CombatState == null)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        foreach (Player p in creature.CombatState.Players)
        {
            if (p.Creature?.Side != CombatSide.Player)
                continue;
            Slifer_the_Sky_Dragon? slifer = SliferSkyDragonService.GetControllingSlifer(p);
            if (slifer == null)
                continue;
            await SliferSkyDragonService.ApplySliferPressureToEnemyAsync(ctx, p, creature, slifer);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player && Owner?.PlayerCombatState != null)
        {
            await MonsterCommandRegistry.ResolveKarateManEndOfTurnDestructionAsync(Owner);
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

        if (!command.IsSingleTargeted || command.ModelSource is not BaseMonsterCard monster)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        var cs = command.Attacker.CombatState;
        if (cs == null)
            return;

        Player? atkOwner = command.Attacker.Player;
        if (monster is Spirit_of_the_Breeze && atkOwner?.Creature != null)
            await CreatureCmd.Heal(atkOwner.Creature, 1m);

        if (monster is D_D_Warrior_Lady warriorLady)
        {
            Creature? wlPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(warriorLady);
            if (wlPet != null)
                MonsterCommandRegistry.GetOrCreate(wlPet).WarriorLadyBanishWindowActive = true;
        }

        bool splinter = monster.AttackDealsSplinterDamage;
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
            _splinterChainRunning = true;
            try
            {
                await ResolveSplinterChainAsync(ctx, command, monster, cs);
            }
            finally
            {
                _splinterChainRunning = false;
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

        if (blighted)
        {
            decimal blightMultiplier = 0.5m;
            if (command.Attacker?.GetPower<SecretPassTreasuresBlightPower>() is { Amount: var secretPassPct })
                blightMultiplier = secretPassPct / 100m;
            else if (monster.AttackDealsFullBlightedDamage)
                blightMultiplier = 1m;
            foreach (DamageResult r in command.Results)
            {
                int hitDamage = FullIncomingDamage(r);
                if (r.Receiver.Side != CombatSide.Enemy || hitDamage <= 0)
                    continue;

                int blight = (int)decimal.Floor(hitDamage * blightMultiplier);
                if (blight <= 0)
                    continue;

                await PowerCmd.Apply<BlightPower>(r.Receiver, blight, command.Attacker, monster);
            }
        }

        // Splinter follow-up hits run nested AfterAttack while _splinterChainRunning; execute-style hooks are skipped there and applied in ResolveSplinterChainAsync via ProcessMonsterExecuteKillEffectsAsync so splinter kills count as that monster's execute.
        if (!_splinterChainRunning)
            await ProcessMonsterExecuteKillEffectsAsync(command, monster, cs);

        Player? atkPlayer = command.Attacker.Player;
        if (atkPlayer?.Creature != null)
        {
            bool anyUnblocked = false;
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side == CombatSide.Enemy && r.UnblockedDamage > 0)
                {
                    anyUnblocked = true;
                    break;
                }
            }

            if (anyUnblocked)
            {
                foreach (BaseEquipSpellCard eq in YgoEquipSpellRegistry.GetEquipsForMonster(monster))
                {
                    if (eq is Cestus_of_Dagla cestus)
                    {
                        await CreatureCmd.Heal(atkPlayer.Creature, cestus.DynamicVars["Mgc"].BaseValue);
                        break;
                    }
                }

                if (monster is The_Bistro_Butcher butcher)
                {
                    int draw = butcher.IsUpgraded ? 2 : 1;
                    await CardPileCmd.Draw(ctx, draw, atkPlayer);
                }

                if (monster is Masked_Sorcerer)
                    await CardPileCmd.Draw(ctx, 1, atkPlayer);
            }
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
    /// Permanent execute ATK, Timeater stun, Shinato Corpse-Blight, Twin-Headed Wolf kill bonuses — keyed on <see cref="DamageResult.WasTargetKilled"/> for this attack command.
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

        if (monster is Timeater)
        {
            bool executed = false;
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                    continue;
                executed = true;
                break;
            }

            if (executed)
            {
                foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive).ToList())
                    await CreatureCmd.Stun(e);
            }
        }

        if (monster is Shinato_King_of_a_Higher_Plane)
        {
            foreach (DamageResult r in command.Results)
            {
                int hitDamage = FullIncomingDamage(r);
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled || hitDamage <= 0)
                    continue;

                int blight = (int)decimal.Floor(hitDamage * 0.5m);
                if (blight <= 0)
                    continue;

                foreach (Creature enemy in cs.HittableEnemies)
                {
                    if (!enemy.IsAlive)
                        continue;
                    await PowerCmd.Apply<BlightPower>(enemy, blight, command.Attacker, monster);
                }
            }
        }

        Player? atkPlayer = command.Attacker.Player;
        if (atkPlayer?.Creature != null && monster is The_Winged_Dragon_of_Ra ra)
        {
            decimal gain = ra.DynamicVars["Mgc2"].BaseValue;
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                    continue;
                await PowerCmd.Apply<RaRebirthPower>(atkPlayer.Creature, gain, atkPlayer.Creature, ra);
                break;
            }
        }

        if (atkPlayer?.Creature != null
            && monster is Twin_Headed_Wolf
            && PlayerControlsAtLeastTwoFiendsOnField(atkPlayer))
        {
            foreach (DamageResult r in command.Results)
            {
                if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                    continue;
                await PowerCmd.Apply<StrengthPower>(command.Attacker, 1m, atkPlayer.Creature, monster);
                await PowerCmd.Apply<ArtifactPower>(command.Attacker, 1m, atkPlayer.Creature, monster);
            }
        }
    }

    /// <summary>
    /// First splinter = half of damage past block on the struck enemy. That amount fans out to <b>each</b> other living enemy (N−1 branches);
    /// each branch then chains independently with min(floor(prev base / 2), floor(past block on last hit / 2)) and round-robin picks for later hops.
    /// </summary>
    private static async Task ResolveSplinterChainAsync(
        BlockingPlayerChoiceContext ctx,
        AttackCommand command,
        BaseMonsterCard monster,
        CombatState cs)
    {
        Creature attacker = command.Attacker;
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

    /// <summary>
    /// <see cref="DamageResult.TotalDamage"/> is block + HP removed only; killing blows store excess in <see cref="DamageResult.OverkillDamage"/>.
    /// </summary>
    private static int FullIncomingDamage(DamageResult r) => r.TotalDamage + r.OverkillDamage;

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
        GraveyardRelic? g = player?.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        if (g != null)
            g._sanctuaryHalveNextSpillToPlayer = true;
    }

    public static void RegisterDragonMonsterDestroyed(Player? player)
    {
        GraveyardRelic? g = player?.Relics.OfType<GraveyardRelic>().FirstOrDefault();
        g?.NotifyDragonMonsterDestroyed();
    }

    private void NotifyDragonMonsterDestroyed() => _dragonMonstersDestroyedThisTurn++;

    /// <summary>Dragon duel monsters destroyed during the current player turn (for Super Rejuvenation). Cleared at the start of your next turn.</summary>
    public int DragonMonstersDestroyedThisTurn => _dragonMonstersDestroyedThisTurn;

    private static bool PlayerControlsAtLeastTwoFiendsOnField(Player player)
    {
        int fiends = 0;
        foreach (BaseMonsterCard c in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (c.DuelMonsterRace == DuelMonsterRace.Fiend)
                fiends++;
        }

        return fiends >= 2;
    }
}
