using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;

public sealed class Little_Chimera : EffectMonsterCard
{
    public Little_Chimera()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 6,
            baseDef: 6,
            baseMgc: 5)
    {
    }

    public override StatEffectTotal GetStatEffect(DuelMonsterAttribute targetAttribute)
    {
        if (targetAttribute == DuelMonsterAttribute.Fire)
            return new StatEffectTotal(BaseMgc, 0); // +5 base, +6 when upgrade
        if (targetAttribute == DuelMonsterAttribute.Water)
            return new StatEffectTotal(BaseMgc-9, 0); // -4 base, -3 when upgrade
        return StatEffectTotal.None;
    }
}

