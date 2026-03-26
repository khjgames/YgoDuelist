using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Jirai_Gumo : EffectMonsterCard
{
    public Jirai_Gumo()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
        DynamicVars["Def"].UpgradeValueBy(4m);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(4m);
    }
}
