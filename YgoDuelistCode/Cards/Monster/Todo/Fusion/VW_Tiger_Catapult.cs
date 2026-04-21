using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Vw_Tiger_Catapult : FusionMonsterCard
{
    public Vw_Tiger_Catapult()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 20,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(V_Tiger_Jet),
            typeof(W_Wing_Catapult))
    {
    }

    public override Type[] BundledCards => new[] { typeof(Vwxyz_Dragon_Catapult_Cannon) };

}
