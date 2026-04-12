using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
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

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Fiend;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Hoshiningen),
        typeof(Luminous_Spark),
        typeof(Yami),
        typeof(Dark_Energy),
        typeof(Axe_of_Despair),
        typeof(Big_Bang_Shot),
        typeof(Black_Pendant),
        typeof(Butterfly_Dagger_Elma),
        typeof(Fusion_Sword_Murasame_Blade),
        typeof(Gravity_Axe_Grarl),
        typeof(Horn_of_Light),
        typeof(Horn_of_the_Unicorn),
        typeof(Lightning_Blade),
        typeof(Mage_Power),
        typeof(Malevolent_Nuzzler),
        typeof(Mask_of_Brutality),
        typeof(Megamorph),
        typeof(United_We_Stand),
        typeof(Yellow_Luster_Shield),
        typeof(The_A_Forces),
        typeof(Rush_Recklessly),
        typeof(The_Reliable_Guardian),
    };

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
        int mgcBonus = YgoStatUpgradeScaling.GetMonsterMgcUpgradeDelta(
            DuelMonsterLevel, YgoCardType, BaseMgc, DuelMonsterStatsAreUnknown);
        DynamicVars["Mgc2"].UpgradeValueBy(mgcBonus);
    }
}

