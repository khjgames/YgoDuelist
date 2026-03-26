using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;

public sealed class Hoshiningen : EffectMonsterCard
{
    private const int Mgc2Base = -4;

    public Hoshiningen()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 5,
            baseDef: 7,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (target.DuelMonsterAttribute == DuelMonsterAttribute.Light)
            return new StatEffectTotal(BaseMgc, 0); // +5 base, +6 when upgrade
        if (target.DuelMonsterAttribute == DuelMonsterAttribute.Dark)
            return new StatEffectTotal(DynamicVars["Mgc2"].BaseValue, 0);
        return StatEffectTotal.None;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", Mgc2Base) });

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        int mgcBonus = YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus(BaseMgc);
        DynamicVars["Mgc2"].UpgradeValueBy(mgcBonus);
    }
}

