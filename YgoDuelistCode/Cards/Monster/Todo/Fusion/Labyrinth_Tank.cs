using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Labyrinth_Tank : FusionMonsterCard
{
    public Labyrinth_Tank()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 24,
            baseDef: 24,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Giga_Tech_Wolf),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Cannon_Soldier))
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
