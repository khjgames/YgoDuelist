using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Localization.DynamicVars;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// YgoDuelist monster: printed ATK/DEF/MGC, combat actions from stats, and dynamic card text.
/// </summary>
/// <remarks>
/// <b>cards.json</b> placeholders (match <see cref="CanonicalVars"/>):
/// <c>Damage</c> (printed ATK), <c>Block</c> / <c>Def</c> (printed DEF), <c>Mgc</c>, <c>CalculatedATK</c>, <c>CalculatedDEF</c>, <c>Stars</c>, <c>Increase</c> (execute ATK delta per kill, cf. <c>TheScythe</c>), <c>NumPortions</c> (<see cref="BaseMonsterCard.AttackPortionCount"/> for Portion text).
/// Use four keys: <c>description</c>, <c>description_combat</c>, <c>description_skill</c>, <c>description_skill_combat</c> (hand-effect monsters also use <c>description_hand_effect</c> / <c>_combat</c>).
/// When <see cref="YgoDuelistCard.UseAlternateUpgradedDescription"/> is true, optional <c>_upgraded</c> variants of each active suffix are resolved when upgraded or in upgrade preview (e.g. <c>description_combat_upgraded</c>).
/// Effect monsters add extra <see cref="DynamicVar"/> names via <c>protected override IEnumerable&lt;DynamicVar&gt; CanonicalVars</c> (often <c>base.CanonicalVars.Concat(...)</c>).
/// Hand-scaling totals feed <see cref="ComputedDecimalVar"/> via <see cref="BaseMonsterCard.GetSecondaryStats"/>.
/// </remarks>

public abstract class NormalMonsterCard : BaseMonsterCard
{
    protected NormalMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace = DuelMonsterRace.Warrior,
        int? duelMonsterAttackPlayEnergyOverride = null,
        int? duelMonsterDefensePlayEnergyOverride = null)
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace,
            duelMonsterAttackPlayEnergyOverride, duelMonsterDefensePlayEnergyOverride)
    {
    }

    public override int GetIntrinsicRecklessCombatSelfDamage() => GetEffectiveDuelMonsterLevel() >= 3 ? 1 : 0;

    public override float GetPackWeightMultiplierAdjusted(float packWeightBeforeAdjustments)
    {
        float packWeightMulti = packWeightBeforeAdjustments;
        if (YgoCardType == YgoCardType.Monster && DuelMonsterLevel <= 4){
            packWeightMulti -= 0.06f; // drop the PackWeightMultiplier for level 4 and below Normal Monsters 6%
        } else if (YgoCardType == YgoCardType.Monster && DuelMonsterLevel >= 5 && DuelMonsterLevel <= 6) {
            packWeightMulti -= 0.09f; // drop the PackWeightMultiplier for level 5 and 6 Normal Monsters 9%
        } else if (YgoCardType == YgoCardType.Monster && DuelMonsterLevel >= 7) {
            packWeightMulti -= 0.03f; // drop the PackWeightMultiplier for level 7 and above Normal Monsters 3%
        } else if (YgoCardType == YgoCardType.EffectMonster) {
            packWeightMulti += 0.02f; // increase the PackWeightMultiplier for Effect Monsters 2%
        } else if (YgoCardType == YgoCardType.FusionMonster) {
            packWeightMulti += 0.08f; // increase the PackWeightMultiplier for Fusion Monsters 8%
        } else if (YgoCardType == YgoCardType.RitualMonster) {
            packWeightMulti += 0.10f; // increase the PackWeightMultiplier for Ritual Monsters 10%
        }
        return packWeightMulti;
    }

    /// <inheritdoc cref="BaseMonsterCard.GetPackWeightMultiplierBase" />
    /// <remarks>
    /// Tier scoring applies only to true normal monsters (<see cref="YgoCardType.Monster"/>).
    /// <see cref="EffectMonsterCard"/> and other subclasses use <see cref="YgoCardType"/> other than Monster and stay at <c>1</c> before fusion-material bonus.
    /// </remarks>
    protected override float GetPackWeightMultiplierBase()
    {
        if (YgoCardType != YgoCardType.Monster)
            return 1f;
        GetDynamicPrintedAtkDef(out int atk, out int def);
        return YgoNormalMonsterPackTier.ComputeCombinedTier(DuelMonsterLevel, atk, def);
    }

    /// <summary>
    /// Auto-enable bulk bundling for low pack-weight true normal monsters.
    /// </summary>
    public override bool BulkBundled => YgoCardType == YgoCardType.Monster && AdjustedPackWeightMultiplier <= 0.63f;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in GetNormalMonsterCoreCanonicalVars())
                yield return v;

            int executeDelta = PermanentAtkDeltaOnEnemyExecute;
            if (executeDelta != 0)
                yield return new IntVar("Increase", executeDelta);
        }
    }

    private IEnumerable<DynamicVar> GetNormalMonsterCoreCanonicalVars()
    {
        yield return new StarsVar(MonsterConduitStarCost);
        yield return new DamageVar((decimal)BaseAtk, ValueProp.Move);
        // Always register BlockVar: engine code (e.g. OnUpgrade) uses DynamicVars.Block; omitting it throws KeyNotFoundException.
        // Attack vs defense presentation is CardType; hiding the Block line in attack mode needs UI work, not omitting this var.
        yield return new BlockVar((decimal)BaseDef, ValueProp.Move);
        yield return new DynamicVar("Def", (decimal)BaseDef);
        yield return new DynamicVar("Mgc", (decimal)BaseMgc);
        yield return new ComputedDecimalVar("CalculatedATK", GetTotalAtkForPreview, (decimal)BaseAtk);
        yield return new ComputedDecimalVar("CalculatedDEF", GetTotalDefForPreview, (decimal)BaseDef);
        yield return new ComputedDecimalVar("NumPortions", YgoDuelistCard.GetNumPortionsDisplayValue, 0m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        ApplySavedExecuteAtkBonusToPrintedDamage();
    }

    /// <summary>One normal summon per turn; special summons (e.g. Monster Reborn) bypass this.</summary>
    //protected override bool IsPlayable =>
    //base.IsPlayable &&
    //(Owner == null || !NormalSummonTracker.HasUsedThisTurn(Owner));
    
    /// <summary>
    /// When true and <see cref="YgoStumblingField"/> is active, this card cannot be played from hand in attack stance
    /// (normal / tribute summon must use defense stance). Special-summon-from-hand effects override this to false.
    /// </summary>
    protected virtual bool StumblingBlocksHandSummonInAttackPosition => true;

    /// <summary>Star cost for display and payment; matches StarsVar base (<see cref="AbstractMonsterCard.MonsterConduitStarCost"/>) in CanonicalVars.</summary>
    public override int CanonicalStarCost => (int)DynamicVars.Stars.BaseValue;

    /// <summary>
    /// <see cref="LegionFiendJesterSpellcasterConduit"/>: Spellcaster normal/tribute summons from hand may ignore conduit while Legion count exceeds waivers used this turn.
    /// </summary>
    public override int CurrentStarCost =>
        LegionFiendJesterSpellcasterConduit.ShouldWaiveConduitStarCostForHandSpellcaster(this)
            ? 0
            : base.CurrentStarCost;

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            if (!CanSummonDuelMonster || Owner == null)
            {
                // Cannot normal summon: block attack/defense hand forms; hand-effect form stays gated by subclass IsPlayable.
                if (Owner != null && SupportsHandEffectForm && !IsHandEffectFormActive)
                    return false;
                return true;
            }

            if (YgoStumblingField.IsActive(Owner)
                && StumblingBlocksHandSummonInAttackPosition
                && Type == CardType.Attack)
                return false;

            int tribute = TributeReleaseCount;
            if (tribute > 0 && !TributeSummonSelection.CanMeetTributeCostForSummon(Owner, this))
                return false;

            if (ReactorSlimeSummonGate.BlocksNonDivineSummons(Owner)
                && GetEffectiveDuelMonsterRace() != DuelMonsterRace.DivineBeast)
                return false;

            return true;
        }
    }

    /// <summary>Attack/defend resolutions per command or hand summon (e.g. <see cref="YgoNarrowPassField"/>, Gray Wing, Tyrant Dragon).</summary>
    protected virtual int GetAttackDefendResolutionCount(Player? player) =>
        YgoNarrowPassField.GetAttackOrDefendResolutionCount(player);

    /// <summary>Command Attack resolution: default <see cref="CombatAction"/>; Dice Jar overrides.</summary>
    public virtual Task RunCommandAttackCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        CombatAction(choiceContext, cardPlay);

    public async Task CombatAction(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int atk = BaseAtk;
        int def = BaseDef;
        if (Owner != null)
        {
            var field = DuelMonsterFieldRegistry
                .GetFieldMonsters(Owner)?
                .ToList() ?? new List<BaseMonsterCard>();
            if (!field.Contains(this))
                field.Add(this);
            var stats = CalcDuelMonsterStats(field);
            atk = stats.Atk;
            def = stats.Def;
        }

        int resolutionCount = GetAttackDefendResolutionCount(Owner);

        // HasRecklessBlockerKeyword only affects which keyword chips render (see BaseMonsterCard.RecklessKeywordStackCountForDisplay); it must not skip gameplay self-damage.
        int recklessSelf = GetTotalRecklessCombatSelfDamage();

        async Task ApplyRecklessSelfDamageIfAnyAsync(int resolutionIndex, int totalResolutions)
        {
            if (recklessSelf <= 0
                || Owner?.Creature == null
                || Owner.PlayerCombatState == null)
                return;

            Creature? selfPet = YgoMpCombatOrder.FirstPetWhere(
                Owner.PlayerCombatState,
                p => DuelMonsterFieldRegistry.HasSourceCard(p, this));

            if (selfPet == null || !selfPet.IsAlive)
                return;

            if (YgoMpDiagnostics.IsMultiplayer)
            {
                YgoMpDiagnostics.VerbosePrint(
                    "RecklessCombat",
                    $"{Id?.Entry} res={resolutionIndex + 1}/{totalResolutions} petCombatId={selfPet.CombatId} dmg={recklessSelf} hp={selfPet.CurrentHp}/{selfPet.MaxHp}");
            }

            await CreatureCmd.Damage(
                choiceContext,
                selfPet,
                recklessSelf,
                ValueProp.Unblockable | ValueProp.Unpowered,
                dealer: null,
                cardSource: this);
        }

        if (Type == CardType.Attack && cardPlay.Target != null)
        {
            List<Creature> attackTargets;
            if (DuelMonsterAttackHitsAllEnemies && Owner?.Creature?.CombatState != null)
            {
                attackTargets = Owner.Creature.CombatState.Enemies.Where(e => e.IsAlive).ToList();
            }
            else
            {
                attackTargets = new List<Creature> { cardPlay.Target };
            }

            await BeforeAttackCombatActionAsync(choiceContext, cardPlay);
            WillSet = false;

            for (int i = 0; i < resolutionCount; i++)
            {
                await ApplyRecklessSelfDamageIfAnyAsync(i, resolutionCount);

                foreach (Creature t in attackTargets)
                {
                    AttackCommand? attackCommand = await YgoPortionDamage.DealMonsterAttackToTargetAsync(
                        choiceContext,
                        this,
                        t,
                        atk,
                        "vfx/vfx_attack_slash");

                    if (attackCommand != null && !YgoPortionedSalvo.ShouldSkipMonsterPerHitCardHook(this))
                        await OnAfterMonsterAttackHitAsync(choiceContext, cardPlay, attackCommand);
                }
            }
        }
        else if (Type == CardType.Skill)
        {
            if (Owner != null && Owner.Creature != null)
            {
                WillSet = false;

                for (int i = 0; i < resolutionCount; i++)
                {
                    await ApplyRecklessSelfDamageIfAnyAsync(i, resolutionCount);

                    await CreatureCmd.GainBlock(
                        Owner.Creature,
                        (decimal)def,
                        ValueProp.Move,
                        cardPlay);

                    await OnAfterGainBlockFromCombatActionAsync(choiceContext, cardPlay, def);
                }
            }
        }
    }

    /// <summary>Runs once before attack damage from <see cref="CombatAction"/> (hand summon in ATK, Command Attack, etc.).</summary>
    protected virtual Task BeforeAttackCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    /// <summary>
    /// After a logical attack from <see cref="CombatAction"/> (once per target per outer resolution when not Portioned;
    /// when <see cref="BaseMonsterCard.AttackPortionCount"/> is 2–5, once after all portion chunks to that target — not once per chunk).
    /// </summary>
    protected virtual Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand) =>
        Task.CompletedTask;

    /// <summary>After <see cref="CreatureCmd.GainBlock"/> from this card’s skill combat action (defend from hand or command).</summary>
    protected virtual Task OnAfterGainBlockFromCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay, int blockGranted) =>
        Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null && CanSummonDuelMonster)
        {
            int tribute = TributeReleaseCount;
            if (NormalSummonHandTributeNeedLock.TryConsume(choiceContext, this, out int lockedTributeNeed))
                tribute = lockedTributeNeed;

            TributeSummonPendingResolution? tributePending = null;

            if (tribute > 0)
            {
                if (!TributeSummonPlayPayload.TryTakePendingForManualPlay(choiceContext, this, out tributePending) || tributePending == null
                    || !TributeSummonSelection.TributeSelectionMeetsCost(
                        this,
                        Owner,
                        tributePending.Pets,
                        tributePending.MausoleumHpTributes,
                        tributePending.MausoleumHpLossTotal,
                        tributePending.RequiredTributeCount))
                {
                    if (YgoPlayPayloadNetKey.TryGetKey(this, out ulong kOid, out uint kIdx))
                    {
                        GD.PrintErr(
                            $"[YgoDuelist][MP][Tribute] OnPlay fallback (no kill/summon): key=({kOid},{kIdx}) card={Id?.Entry} pendingNull={tributePending == null} printedLevel={DuelMonsterLevel} effectiveLevel={GetEffectiveDuelMonsterLevel()} tributeNeed={TributeReleaseCount}");
                    }

                    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);

                    if (!ShouldSkipCombatActionAfterSummon(cardPlay))
                    {
                        await ApplyNarrowPassHandSummonCombatPaymentAsync(choiceContext);
                        await CombatAction(choiceContext, cardPlay);
                    }

                    return;
                }

                List<BaseMonsterCard> tributeMonsters = YgoMpCombatOrder
                    .CreatureListOrderedByCombatId(tributePending.Pets)
                    .Select(pet => DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet))
                    .Where(monster => monster != null)
                    .Cast<BaseMonsterCard>()
                    .ToList();

                await OnBeforeTributeMaterialsReleased(choiceContext, cardPlay, tributePending);

                foreach (Creature pet in YgoMpCombatOrder.CreatureListOrderedByCombatId(tributePending.Pets))
                    await CreatureCmd.Kill(pet, force: true);

                int hpLoss = tributePending.MausoleumHpLossTotal;
                if (hpLoss > 0 && Owner.Creature != null)
                {
                    int nextHp = Owner.Creature.CurrentHp - hpLoss;
                    if (nextHp < 0)
                        nextHp = 0;

                    await CreatureCmd.SetCurrentHp(Owner.Creature, nextHp);
                }

                OnBeforeDuelMonsterSummon(choiceContext, cardPlay, tributePending);

                bool summoned = await DuelMonsterSummon.TrySummonDuelMonster(
                    Owner,
                    this,
                    choiceContext,
                    SpecialSummonGrantsImmediateCommandsThisTurn);

                if (summoned)
                {
                    await YgoTributeSummonCardHooks.DispatchOnTributeSummonedMonsterAsync(
                        choiceContext,
                        Owner,
                        this,
                        tributeMonsters);
                }
            }
            else
            {
                OnBeforeDuelMonsterSummon(choiceContext, cardPlay, tributePending);

                await DuelMonsterSummon.TrySummonDuelMonster(
                    Owner,
                    this,
                    choiceContext,
                    SpecialSummonGrantsImmediateCommandsThisTurn);
            }
        }

        if (Owner == null)
            return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);

        await ApplyNarrowPassHandSummonCombatPaymentAsync(choiceContext);
        await CombatAction(choiceContext, cardPlay);
        await OnAfterMonsterPlayResolved(choiceContext, cardPlay);
    }

    /// <summary>Called after summon + combat action resolves (attack or defend from hand).</summary>
    protected virtual Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    /// <summary>Same life cost as <see cref="Command.Command_Attack"/> / <see cref="Command.Command_Defend"/> before hand summon combat (ATK/DEF).</summary>
    private async Task ApplyNarrowPassHandSummonCombatPaymentAsync(PlayerChoiceContext choiceContext)
    {
        if (Owner?.PlayerCombatState == null)
            return;
        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            Owner.PlayerCombatState,
            p => DuelMonsterFieldRegistry.HasSourceCard(p, this));
        await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, Owner, pet);
    }

    /// <summary>
    /// After tribute selection is validated, before tribute monsters are destroyed (materials still on the field).
    /// </summary>
    protected virtual Task OnBeforeTributeMaterialsReleased(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        TributeSummonPendingResolution pending) =>
        Task.CompletedTask;

    /// <summary>After tribute releases resolve, before <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> (e.g. Gate Guardian MGC bonus).</summary>
    protected virtual void OnBeforeDuelMonsterSummon(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        TributeSummonPendingResolution? tributePending)
    {
    }

    /// <summary>Tribute fallback path: skip the post-summon combat action when true (e.g. Dark Zebra alone on field).</summary>
    protected virtual bool ShouldSkipCombatActionAfterSummon(CardPlay cardPlay) => false;

    // Complex: override in effect monsters for calculated damage.
    // public virtual decimal GetDamageAmount(Creature? target) => DynamicVars.Damage.BaseValue;

    protected override void OnUpgrade()
    {
        int atkBonus = YgoStatUpgradeScaling.GetMonsterPrintedLineUpgradeDelta(
            DuelMonsterLevel,
            YgoCardType,
            MonsterEnergyCostCalculator.GetMonsterPlayEnergy(
                DuelMonsterLevel, YgoCardType, BaseAtk, true, false, DuelMonsterStatsAreUnknown),
            BaseAtk);

        int defBonus = YgoStatUpgradeScaling.GetMonsterPrintedLineUpgradeDelta(
            DuelMonsterLevel,
            YgoCardType,
            MonsterEnergyCostCalculator.GetMonsterPlayEnergy(
                DuelMonsterLevel, YgoCardType, BaseDef, false, false, DuelMonsterStatsAreUnknown),
            BaseDef,
            isDefenseLine: true);

        int mgcBonus = YgoStatUpgradeScaling.GetMonsterMgcUpgradeDelta(
            DuelMonsterLevel, YgoCardType, BaseMgc, DuelMonsterStatsAreUnknown);

        DynamicVars.Damage.UpgradeValueBy(atkBonus);
        DynamicVars["Def"].UpgradeValueBy(defBonus);
        // Use ContainsKey: get_Block throws KeyNotFoundException if BlockVar was omitted from canonical vars.
        if (DynamicVars.ContainsKey("Block"))
            DynamicVars["Block"].UpgradeValueBy(defBonus);

        DynamicVars["Mgc"].UpgradeValueBy(mgcBonus);
        SyncPermanentExecuteIncreaseVar();
    }

    public static decimal GetTotalAtkForPreview(CardModel card)
    {
        if (card is not BaseMonsterCard monster)
            return 0;

        // Owner asserts mutable; canonical templates (card library, compendium) must not read it.
        List<BaseMonsterCard> field;
        if (card.IsCanonical)
            field = new List<BaseMonsterCard> { monster };
        else
        {
            var owner = card.Owner;
            field = DuelMonsterFieldRegistry
                .GetFieldMonsters(owner)?
                .ToList() ?? new List<BaseMonsterCard>();
            // For preview, pretend this monster is also on the field
            // so its own aura (GetStatEffect) applies to itself.
            if (!field.Contains(monster))
                field.Add(monster);
        }

        var stats = monster.CalcDuelMonsterStats(field);
        return stats.Atk;
    }

    public static decimal GetTotalDefForPreview(CardModel card)
    {
        if (card is not BaseMonsterCard monster)
            return 0;

        List<BaseMonsterCard> field;
        if (card.IsCanonical)
        {
            field = new List<BaseMonsterCard> { monster };
        }
        else
        {
            var owner = card.Owner;
            field = DuelMonsterFieldRegistry
                .GetFieldMonsters(owner)?
                .ToList() ?? new List<BaseMonsterCard>();

            if (!field.Contains(monster))
                field.Add(monster);
        }

        var stats = monster.CalcDuelMonsterStats(field);
        return stats.Def;
    }

    /// <summary>
    /// <see cref="Activate_Effect"/> playability with command-card owner (earth tribute, field gates). Default: <see cref="IMonsterActivatedEffect.IsActivatedEffectAvailable"/>.
    /// </summary>
    public virtual bool IsActivatedEffectAvailableInCommandContext(Player? commandOwner)
    {
        if (this is not IMonsterActivatedEffect fx)
            return false;
        return fx.IsActivatedEffectAvailable;
    }
}
