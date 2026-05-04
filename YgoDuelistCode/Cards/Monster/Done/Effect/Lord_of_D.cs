using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While on the field: Dragon-type monsters gain +{Mgc} ATK and +{Mgc} DEF (base 2, upgraded 5).</summary>
public sealed class Lord_of_D : EffectMonsterCard
{
    private const int DragonAuraMgcUpgraded = 5;

    public Lord_of_D()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 11,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dragon | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster;
    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (target.DuelMonsterRace != DuelMonsterRace.Dragon)
            return StatEffectTotal.None;
        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(mgc, mgc);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        int fromMonsterScaling = BaseMgc + YgoStatUpgradeScaling.GetMonsterMgcUpgradeDelta(
            DuelMonsterLevel, YgoCardType, BaseMgc, DuelMonsterStatsAreUnknown);
        DynamicVars["Mgc"].UpgradeValueBy(DragonAuraMgcUpgraded - fromMonsterScaling);
    }
}
