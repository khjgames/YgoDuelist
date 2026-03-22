using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Pragtical : FusionMonsterCard
{
    public Pragtical()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 19,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dinosaur)
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
