using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Giltia_the_D_Knight : FusionMonsterCard
{
    public Giltia_the_D_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 18,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Guardian_of_the_Labyrinth),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Protector_of_the_Throne))
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
