using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Hourglass_of_Courage : EffectMonsterCard
{
    public Hourglass_of_Courage()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 11,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Normal;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Hourglass_of_Courage),
    };

    protected override StatEffectTotalMultiplier GetSelfStatMultiplier()
    {
        if (!NormalSummonHalveTimerActive())
            return StatEffectTotalMultiplier.Identity;
        return StatEffectTotalMultiplier.HourglassOfCourageNormalSummon;
    }

    private bool NormalSummonHalveTimerActive()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in Owner.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) != this)
                continue;
            return pet.HasPower<HourglassOfCourageHalvedPower>();
        }

        return false;
    }
}
