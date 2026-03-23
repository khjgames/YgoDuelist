using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Dark_Balter_the_Terrible : FusionMonsterCard
{
    public Dark_Balter_the_Terrible()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Possessed_Dark_Soul),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Frontier_Wiseman))
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
