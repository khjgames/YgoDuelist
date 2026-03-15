using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;

public sealed class Milus_Radiant : EffectMonsterCard
{
    public Milus_Radiant()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 3,
            baseDef: 3,
            baseMgc: 5)
    {
    }

    public override StatEffectTotal GetStatEffect(DuelMonsterAttribute targetAttribute)
    {
        if (targetAttribute == DuelMonsterAttribute.Earth)
            return new StatEffectTotal(BaseMgc, 0); // +5 base, +6 when upgrade
        if (targetAttribute == DuelMonsterAttribute.Wind)
            return new StatEffectTotal(BaseMgc-9, 0); // -4 base, -3 when upgrade
        return StatEffectTotal.None;
    }
}

