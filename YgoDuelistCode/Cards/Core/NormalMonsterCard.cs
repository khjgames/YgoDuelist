using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Localization.DynamicVars;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// YgoDuelist monster: printed ATK/DEF/MGC, combat actions from stats, and dynamic card text.
/// </summary>
/// <remarks>
/// <b>cards.json</b> placeholders (match <see cref="CanonicalVars"/>):
/// <c>Damage</c> (printed ATK), <c>Block</c> / <c>Def</c> (printed DEF), <c>Mgc</c>, <c>CalculatedATK</c>, <c>CalculatedDEF</c>, <c>Stars</c>, <c>Increase</c> (execute ATK delta per kill, cf. <c>TheScythe</c>).
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
        yield return new BlockVar((decimal)BaseDef, ValueProp.Move);
        yield return new DynamicVar("Def", (decimal)BaseDef);
        yield return new DynamicVar("Mgc", (decimal)BaseMgc);
        yield return new ComputedDecimalVar("CalculatedATK", GetTotalAtkForPreview, (decimal)BaseAtk);
        yield return new ComputedDecimalVar("CalculatedDEF", GetTotalDefForPreview, (decimal)BaseDef);
    }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        ApplySavedExecuteAtkBonusToPrintedDamage();
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        ApplySavedExecuteAtkBonusToPrintedDamage();
        SyncPermanentExecuteIncreaseVar();
    }

    /// <summary>One normal summon per turn; special summons (e.g. Monster Reborn) bypass this.</summary>
    //protected override bool IsPlayable =>
    //base.IsPlayable &&
    //(Owner == null || !NormalSummonTracker.HasUsedThisTurn(Owner));
    
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
                return true;

            int tribute = TributeReleaseCount;
            if (tribute > 0 && TributeSummonSelection.CountTributableFieldMonsters(Owner) < tribute)
                return false;

            return DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tribute);
        }
    }

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

        if (Type == CardType.Attack && cardPlay.Target != null)
        {
            WillSet = false;
            await DamageCmd.Attack((decimal)atk)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
        else if (Type == CardType.Skill)
        {
            if (Owner != null && Owner.Creature != null)
            {
                WillSet = false;
                await CreatureCmd.GainBlock(
                    Owner.Creature,
                    (decimal)def,
                    ValueProp.Move,
                    cardPlay);
                await OnAfterGainBlockFromCombatActionAsync(choiceContext, cardPlay, def);
            }
        }
    }

    /// <summary>After <see cref="CreatureCmd.GainBlock"/> from this card’s skill combat action (defend from hand or command).</summary>
    protected virtual Task OnAfterGainBlockFromCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay, int blockGranted) =>
        Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null && CanSummonDuelMonster)
        {
            int tribute = TributeReleaseCount;
            if (tribute > 0)
            {
                if (!TributeSummonPlayPayload.TryTakePending(this, out var mats) || mats == null || mats.Count < tribute)
                {
                    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);
                    if (!ShouldSkipCombatActionAfterSummon(cardPlay))
                        await CombatAction(choiceContext, cardPlay);
                    return;
                }

                foreach (Creature pet in mats)
                    await CreatureCmd.Kill(pet, force: true);
            }

            await DuelMonsterSummon.TrySummonDuelMonster(Owner, this, choiceContext);
        }

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);

        await CombatAction(choiceContext, cardPlay);
        await OnAfterMonsterPlayResolved(choiceContext, cardPlay);
    }

    /// <summary>Called after summon + combat action resolves (attack or defend from hand).</summary>
    protected virtual Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    /// <summary>Tribute fallback path: skip the post-summon combat action when true (e.g. Dark Zebra alone on field).</summary>
    protected virtual bool ShouldSkipCombatActionAfterSummon(CardPlay cardPlay) => false;

    // Complex: override in effect monsters for calculated damage.
    // public virtual decimal GetDamageAmount(Creature? target) => DynamicVars.Damage.BaseValue;

    protected override void OnUpgrade()
    {
        int atkBonus = SuppressPrintedAttackUpgradeForEfficiencyTax
            ? 0
            : YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus(BaseAtk);
        int defBonus = SuppressPrintedDefenseUpgradeForEfficiencyTax
            ? 0
            : YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus(BaseDef);
        int mgcBonus = YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus(BaseMgc);
        DynamicVars.Damage.UpgradeValueBy(atkBonus);
        DynamicVars["Def"].UpgradeValueBy(defBonus);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(defBonus);
        DynamicVars["Mgc"].UpgradeValueBy(mgcBonus);
        SyncPermanentExecuteIncreaseVar();
    }

    public static decimal GetTotalAtkForPreview(CardModel card)
    {
        if (card is not BaseMonsterCard monster)
            return 0;

        var owner = card.Owner;
        var field = DuelMonsterFieldRegistry
            .GetFieldMonsters(owner)?
            .ToList() ?? new List<BaseMonsterCard>();

        // For preview, pretend this monster is also on the field
        // so its own aura (GetStatEffect) applies to itself.
        if (!field.Contains(monster))
            field.Add(monster);

        var stats = monster.CalcDuelMonsterStats(field);
        return stats.Atk;
    }

    public static decimal GetTotalDefForPreview(CardModel card)
    {
        if (card is not BaseMonsterCard monster)
            return 0;

        var owner = card.Owner;
        var field = DuelMonsterFieldRegistry
            .GetFieldMonsters(owner)?
            .ToList() ?? new List<BaseMonsterCard>();

        if (!field.Contains(monster))
            field.Add(monster);

        var stats = monster.CalcDuelMonsterStats(field);
        return stats.Def;
    }
}
