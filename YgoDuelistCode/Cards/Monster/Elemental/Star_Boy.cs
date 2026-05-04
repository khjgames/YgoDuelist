using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;

public sealed class Star_Boy : EffectMonsterCard
{
    private const int Mgc2Base = -4;

    public Star_Boy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 5,
            baseDef: 5,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Fish)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Ocean;
    public override Type[] RelatedCards => GetRelatedCards();

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (target.DuelMonsterAttribute == DuelMonsterAttribute.Water)
            return new StatEffectTotal(BaseMgc, 0); // +5 base, +6 when upgrade
        if (target.DuelMonsterAttribute == DuelMonsterAttribute.Fire)
            return new StatEffectTotal(DynamicVars["Mgc2"].BaseValue, 0);
        return StatEffectTotal.None;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", Mgc2Base) });

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        int mgcBonus = YgoStatUpgradeScaling.GetMonsterMgcUpgradeDelta(
            DuelMonsterLevel, YgoCardType, BaseMgc, DuelMonsterStatsAreUnknown);
        DynamicVars["Mgc2"].UpgradeValueBy(mgcBonus);
    }
}
