using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Aqua_Dragon : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Aqua_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 22,
            baseDef: 19,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.SeaSerpent,
            fusionMaterialTypes: new[]
            {
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Fairy_Dragon),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Amazon_of_the_Seas),
                typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Zone_Eater)
            }
            )
    {
    }
}
