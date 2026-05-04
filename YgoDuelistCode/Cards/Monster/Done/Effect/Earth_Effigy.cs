using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Earth_Effigy : EffectMonsterCard, IDoubleTributeMaterial
{
    public Earth_Effigy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 1,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RequiresNormalMonster = true,
        RestrictAttribute = true,
        RequiredAttribute = DuelMonsterAttribute.Earth
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Normal;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Earth_Effigy), DoubleTributeTargetSpec);
}
