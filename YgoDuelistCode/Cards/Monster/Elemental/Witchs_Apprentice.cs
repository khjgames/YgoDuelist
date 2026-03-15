using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;

public sealed class Witchs_Apprentice : EffectMonsterCard
{
    public Witchs_Apprentice()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 6,
            baseDef: 5,
            baseMgc: 5)
    {
    }

    public override StatEffectTotal GetStatEffect(DuelMonsterAttribute targetAttribute)
    {
        if (targetAttribute == DuelMonsterAttribute.Dark)
            return new StatEffectTotal(BaseMgc, 0); // +5 base, +6 when upgrade
        if (targetAttribute == DuelMonsterAttribute.Light)
            return new StatEffectTotal(BaseMgc-9, 0); // -4 base, -3 when upgrade
        return StatEffectTotal.None;
    }
}

