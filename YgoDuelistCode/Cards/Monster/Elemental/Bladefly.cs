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

public sealed class Bladefly : EffectMonsterCard
{
    private const int Mgc2Base = -4;

    public Bladefly()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 6,
            baseDef: 7,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Insect;
    public override Type[] RelatedCards => GetRelatedCards();

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (target.DuelMonsterAttribute == DuelMonsterAttribute.Wind)
            return new StatEffectTotal(BaseMgc, 0); // +5 base, +6 when upgrade
        if (target.DuelMonsterAttribute == DuelMonsterAttribute.Earth)
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
