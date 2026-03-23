using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Dark_Blade_the_Dragon_Knight : FusionMonsterCard
{
    public Dark_Blade_the_Dragon_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 22,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Dark_Blade),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Pitch_Dark_Dragon))
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
