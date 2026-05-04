using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Chronomaly_Mayan_Machine : EffectMonsterCard, IDoubleTributeMaterial
{
    public Chronomaly_Mayan_Machine()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 15,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public DoubleTributeSummonTargetSpec DoubleTributeTargetSpec => new()
    {
        RestrictRace = true,
        RequiredRace = DuelMonsterRace.Machine
    };

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Machine;

    public override Type[] RelatedCards =>
        DoubleTributeRelatedCards.For(typeof(Chronomaly_Mayan_Machine), DoubleTributeTargetSpec);
}
