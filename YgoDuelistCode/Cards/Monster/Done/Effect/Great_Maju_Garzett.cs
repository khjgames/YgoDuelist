using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Great_Maju_Garzett : EffectMonsterCard
{
    public Great_Maju_Garzett()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[]
    {
        typeof(Great_Maju_Garzett),
    };

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

                List<Creature> orderedTributes = YgoMpCombatOrder.CreatureListOrderedByCombatId(pending.Pets);
                int tributePrintedAtk = orderedTributes.Count > 0 ? GetTributePrintedAtk(orderedTributes[0]) : 0;
                int doubled = Math.Clamp(tributePrintedAtk * 2, 0, 9999) + PermanentAtkBonusFromExecutes;
                if (doubled > 9999)
                    doubled = 9999;
                DynamicVars.Damage.BaseValue = doubled;

                foreach (Creature pet in orderedTributes)
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

    private static int GetTributePrintedAtk(Creature pet)
    {
        if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard src)
            return 0;
        if (src.DynamicVars?.Damage != null)
            return (int)src.DynamicVars.Damage.BaseValue;
        return src.BaseAtk;
    }
}
