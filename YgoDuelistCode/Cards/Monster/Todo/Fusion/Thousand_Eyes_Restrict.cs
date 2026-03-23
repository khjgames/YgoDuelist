using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Thousand_Eyes_Restrict : FusionMonsterCard
{
    public Thousand_Eyes_Restrict()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual.Relinquished),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Thousand_Eyes_Idol))
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
