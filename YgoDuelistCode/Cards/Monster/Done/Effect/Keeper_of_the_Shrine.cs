using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Keeper_of_the_Shrine : EffectMonsterCard, IDoubleTributeMaterial
{
    public Keeper_of_the_Shrine()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 0,
            baseDef: 21,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictRace = true,
        RequiredRace = DuelMonsterRace.Dragon
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Dragon;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Keeper_of_the_Shrine), DoubleTributeTargetSpec);
}
