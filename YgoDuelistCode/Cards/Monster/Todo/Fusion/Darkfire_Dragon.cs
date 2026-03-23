using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Darkfire_Dragon : FusionMonsterCard
{
    public Darkfire_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 15,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Firegrass),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Petit_Dragon))
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
