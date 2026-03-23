using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Blue_Eyes_Ultimate_Dragon : FusionMonsterCard
{
    public Blue_Eyes_Ultimate_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 12,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 45,
            baseDef: 38,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Blue_Eyes_White_Dragon),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Blue_Eyes_White_Dragon),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Blue_Eyes_White_Dragon))
    {
    }

    protected override void OnUpgrade()
    {
        ApplyCardEffectPlaceholder();
    }

    private void ApplyCardEffectPlaceholder()
    {
    }
}
