using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

/// <summary>Equip: +{Mgc} ATK; equipped monster's attacks deal Splinter (piercing) splash.</summary>
public sealed class Big_Bang_Shot : BaseEquipSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 4m) };

    public Big_Bang_Shot()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Big_Bang_Shot),
    };

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

    public override bool GrantsSplinterTo(BaseMonsterCard equipped) => true;

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(4m);
}
