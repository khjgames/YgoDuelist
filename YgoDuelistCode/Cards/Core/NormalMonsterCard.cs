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
/// ZGO normal monster: deals ATK as damage. Basic structure only — single DamageVar.
/// (Complex calculated damage / GetDamageAmount override commented out for temp art.)
/// </summary>

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
        DuelMonsterRace duelMonsterRace = DuelMonsterRace.Warrior)
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new StarsVar(1),
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
    
    /// <summary>Star cost for display and payment; matches StarsVar in CanonicalVars.</summary>
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
            if (tribute <= 0)
                return true;
            return TributeMaterialMarkTracker.CanSatisfyTribute(Owner, tribute);
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
                IReadOnlyList<Creature> mats = await TributeMaterialMarkTracker.ConsumeTributeMaterialsAsync(Owner, tribute);
                if (mats.Count < tribute)
                {
                    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);
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
    }

    // Complex: override in effect monsters for calculated damage.
    // public virtual decimal GetDamageAmount(Creature? target) => DynamicVars.Damage.BaseValue;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Def"].UpgradeValueBy(1m);
        DynamicVars["Mgc"].UpgradeValueBy(1m);
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
