using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Superheavy_Samurai_Big_Waraji : EffectMonsterCard, IDoubleTributeMaterial
{
    public Superheavy_Samurai_Big_Waraji()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictRace = true,
        RequiredRace = DuelMonsterRace.Machine
    };

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Machine;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Superheavy_Samurai_Big_Waraji), DoubleTributeTargetSpec);
}
