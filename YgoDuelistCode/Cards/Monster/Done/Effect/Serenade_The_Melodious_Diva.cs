using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Serenade_The_Melodious_Diva : EffectMonsterCard, IDoubleTributeMaterial
{
    public Serenade_The_Melodious_Diva()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 4,
            baseDef: 19,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictRace = true,
        RequiredRace = DuelMonsterRace.Fairy
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Light;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Serenade_The_Melodious_Diva), DoubleTributeTargetSpec);
}
