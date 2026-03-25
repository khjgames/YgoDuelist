using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Card_4_Starred_Ladybug_of_Doom : EffectMonsterCard
{
    public Card_4_Starred_Ladybug_of_Doom()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 8,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }
}
