using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When sent from hand to the Graveyard, draw cards equal to printed <c>Mgc</c> (same as <see cref="Electric_Snake"/>).</summary>
public sealed class Elephant_Statue_of_Blessing : EffectMonsterCard
{
    public Elephant_Statue_of_Blessing()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 15,
            baseDef: 0,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Elephant_Statue_of_Blessing) };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
