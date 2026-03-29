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

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Normal;

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
