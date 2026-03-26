using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
/// <c>Damage</c> (printed ATK), <c>Block</c> / <c>Def</c> (printed DEF), <c>Mgc</c>, <c>CalculatedATK</c>, <c>CalculatedDEF</c>, <c>Stars</c>.
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

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new StarsVar(MonsterConduitStarCost),
        new DamageVar((decimal)BaseAtk, ValueProp.Move),
        new BlockVar((decimal)BaseDef, ValueProp.Move),
        new DynamicVar("Def", (decimal)BaseDef),
        new DynamicVar("Mgc", (decimal)BaseMgc),
        // Combat preview totals (base + own effect + field auras).
        new ComputedDecimalVar("CalculatedATK", GetTotalAtkForPreview, (decimal)BaseAtk),
        new ComputedDecimalVar("CalculatedDEF", GetTotalDefForPreview, (decimal)BaseDef)
    };

    /// <summary>One normal summon per turn; special summons (e.g. Monster Reborn) bypass this.</summary>
    //protected override bool IsPlayable =>
    //base.IsPlayable &&
    //(Owner == null || !NormalSummonTracker.HasUsedThisTurn(Owner));
    
    /// <summary>Star cost for display and payment; matches StarsVar base (<see cref="AbstractMonsterCard.MonsterConduitStarCost"/>) in CanonicalVars.</summary>
    public override int CanonicalStarCost => (int)DynamicVars.Stars.BaseValue;

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
            }
        }
    }

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
        int atkBonus = YgoStatUpgradeScaling.GetStatUpgradeBonus(BaseAtk);
        int defBonus = YgoStatUpgradeScaling.GetStatUpgradeBonus(BaseDef);
        int mgcBonus = YgoStatUpgradeScaling.GetStatUpgradeBonus(BaseMgc);
        DynamicVars.Damage.UpgradeValueBy(atkBonus);
        DynamicVars["Def"].UpgradeValueBy(defBonus);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(defBonus);
        DynamicVars["Mgc"].UpgradeValueBy(mgcBonus);
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
