using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>End Phase: returns to hand. Upgraded: Retain.</summary>
public sealed class The_Wicked_Worm_Beast : EffectMonsterCard
{
    public override bool UseAlternateUpgradedDescription => true;

    public The_Wicked_Worm_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 14,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 0)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Normal;

    public override Type[] RelatedCards => new[] { typeof(The_Wicked_Worm_Beast) };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Concat(
            IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None
                ? new[] { CardKeyword.Retain }
                : Enumerable.Empty<CardKeyword>());

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(
            IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None
                ? new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) }
                : Enumerable.Empty<IHoverTip>());
}
