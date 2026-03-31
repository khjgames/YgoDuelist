using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Black_Tyranno : EffectMonsterCard
{
    public Black_Tyranno()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 26,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dinosaur)
    {
    }

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

}
