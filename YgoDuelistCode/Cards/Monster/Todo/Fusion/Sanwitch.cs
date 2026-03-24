using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Sanwitch : FusionMonsterCard
{
    public Sanwitch()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 21,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Sangan),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Witch_of_the_Black_Forest))
    {
    }

}
