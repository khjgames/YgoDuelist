using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>GY end-phase Special Summon — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoManticoreOfDarknessEndPhase"/>.</summary>
public sealed class Manticore_of_Darkness : EffectMonsterCard
{
    public Manticore_of_Darkness()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 23,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Fire | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Manticore_of_Darkness) };
}
