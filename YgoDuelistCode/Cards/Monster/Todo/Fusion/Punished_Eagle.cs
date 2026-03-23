using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Punished_Eagle : FusionMonsterCard
{
    public Punished_Eagle()
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
            duelMonsterRace: DuelMonsterRace.WingedBeast,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Blue_Winged_Crown),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Niwatori))
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
