using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Printed ATK/DEF become the combined printed ATK and DEF of the monsters Tributed for its Tribute Summon.</summary>
public sealed class Maju_Garzett : EffectMonsterCard
{
    public Maju_Garzett()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: -1,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Maju_Garzett) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null && CanSummonDuelMonster)
        {
            int tribute = TributeReleaseCount;
            if (tribute > 0)
            {
                if (!TributeSummonPlayPayload.TryTakePendingForManualPlay(choiceContext, this, out var pending) || pending == null
                    || !TributeSummonSelection.TributeSelectionMeetsCost(
                        this,
                        Owner,
                        pending.Pets,
                        pending.MausoleumHpTributes,
                        pending.MausoleumHpLossTotal))
                {
                    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);
                    if (!ShouldSkipCombatActionAfterSummon(cardPlay))
                        await CombatAction(choiceContext, cardPlay);
                    return;
                }

                int sumAtk = 0;
                int sumDef = 0;
                foreach (Creature pet in pending.Pets)
                {
                    if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not BaseMonsterCard src)
                        continue;
                    sumAtk += GetPrintedAtk(src);
                    sumDef += GetPrintedDef(src);
                }

                sumAtk = Math.Clamp(sumAtk, 0, 9999);
                sumDef = Math.Clamp(sumDef, 0, 9999);
                DynamicVars.Damage.BaseValue = sumAtk;
                DynamicVars["Def"].BaseValue = sumDef;
                if (DynamicVars.ContainsKey("Block"))
                    DynamicVars["Block"].BaseValue = sumDef;

                foreach (Creature pet in pending.Pets)
                    await CreatureCmd.Kill(pet, force: true);

                int hpLoss = pending.MausoleumHpLossTotal;
                if (hpLoss > 0 && Owner.Creature != null)
                {
                    int nextHp = Owner.Creature.CurrentHp - hpLoss;
                    if (nextHp < 0)
                        nextHp = 0;
                    await CreatureCmd.SetCurrentHp(Owner.Creature, nextHp);
                }
            }

            await DuelMonsterSummon.TrySummonDuelMonster(Owner, this, choiceContext);
        }

        if (Owner?.Creature != null)
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);

        await CombatAction(choiceContext, cardPlay);
        await OnAfterMonsterPlayResolved(choiceContext, cardPlay);
    }

    private static int GetPrintedAtk(BaseMonsterCard src)
    {
        if (src.DynamicVars?.Damage != null)
            return (int)src.DynamicVars.Damage.BaseValue;
        return src.BaseAtk;
    }

    private static int GetPrintedDef(BaseMonsterCard src)
    {
        if (src.DynamicVars != null && src.DynamicVars.ContainsKey("Def"))
            return (int)src.DynamicVars["Def"].BaseValue;
        return src.BaseDef;
    }
}
