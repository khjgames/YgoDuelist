using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Bickuribox : FusionMonsterCard
{
    public Bickuribox()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 23,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Crass_Clown),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Dream_Clown))
    {
    }

}
