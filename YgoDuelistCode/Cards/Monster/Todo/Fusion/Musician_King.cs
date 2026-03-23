using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Musician_King : FusionMonsterCard
{
    public Musician_King()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 17,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Witch_of_the_Black_Forest),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Lady_of_Faith))
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
