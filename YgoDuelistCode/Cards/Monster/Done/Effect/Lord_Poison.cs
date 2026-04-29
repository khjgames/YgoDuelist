using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Lord_Poison : EffectMonsterCard
{
    public override int AttackPortionCount => 2;
    public Lord_Poison()
        : base(
            cost: 1,
            type: global::MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack,
            rarity: global::MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Uncommon,
            target: global::MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterRace.Plant)
    {
    }

    protected override bool UsesBattleDeathGraveyardMark => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Water | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Lord_Poison) };
}
