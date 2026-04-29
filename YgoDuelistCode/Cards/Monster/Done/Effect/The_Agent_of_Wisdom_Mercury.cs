using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While The Sanctuary in the Sky is face-up, draw 1 at the start of your turn.</summary>
public sealed class The_Agent_of_Wisdom_Mercury : EffectMonsterCard
{
    public The_Agent_of_Wisdom_Mercury()
        : base(
            cost: 1,
            type: global::MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack,
            rarity: global::MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Common,
            target: global::MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterAttribute.Light,
            baseAtk: 0,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(The_Agent_of_Wisdom_Mercury), typeof(The_Sanctuary_in_the_Sky) };

    public override bool ParticipatesInSanctuaryMercuryDraw => true;
}
