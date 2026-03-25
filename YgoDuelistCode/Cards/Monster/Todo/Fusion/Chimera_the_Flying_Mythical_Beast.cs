using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Chimera_the_Flying_Mythical_Beast : FusionMonsterCard
{
    public Chimera_the_Flying_Mythical_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 21,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Gazelle_the_King_of_Mythical_Beasts),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Berfomet))
    {
    }
}
