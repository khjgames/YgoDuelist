using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Castle_of_Dark_Illusions : EffectMonsterCard
{
    public Castle_of_Dark_Illusions()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 9,
            baseDef: 19,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie | YgoCardPackTags.Fiend;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.ZombieBoost;

    public override Type[] RelatedCards => GetRelatedCards();

    public override Type[] BundledCards => new[] { typeof(Pumpking_the_King_of_Ghosts) };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5;
    }

}
