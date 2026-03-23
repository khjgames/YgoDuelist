using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Mokey_Mokey_King : FusionMonsterCard
{
    public Mokey_Mokey_King()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 3,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Mokey_Mokey),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Mokey_Mokey),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Mokey_Mokey))
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
