using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

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
            duelMonsterAttackPlayEnergyOverride: 1)
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
                if (!TributeSummonPlayPayload.TryTakePending(this, out var mats) || mats == null || mats.Count < tribute)
                {
                    await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.AttackAnimDelay);
                    if (!ShouldSkipCombatActionAfterSummon(cardPlay))
                        await CombatAction(choiceContext, cardPlay);
                    return;
                }

                Creature primaryTribute = mats[0];
                int tributePrintedAtk = GetTributePrintedAtk(primaryTribute);
                int doubled = Math.Clamp(tributePrintedAtk * 2, 0, 9999) + PermanentAtkBonusFromExecutes;
                if (doubled > 9999)
                    doubled = 9999;
                DynamicVars.Damage.BaseValue = doubled;

                foreach (Creature pet in mats)
                    await CreatureCmd.Kill(pet, force: true);
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
        if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not BaseMonsterCard src)
            return 0;
        if (src.DynamicVars?.Damage != null)
            return (int)src.DynamicVars.Damage.BaseValue;
        return src.BaseAtk;
    }
}
