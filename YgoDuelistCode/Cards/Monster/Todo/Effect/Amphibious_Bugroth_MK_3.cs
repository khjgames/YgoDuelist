using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Amphibious_Bugroth_MK_3 : EffectMonsterCard
{
    public Amphibious_Bugroth_MK_3()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 13,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

}
