using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Black_Pendant : BaseEquipSpellCard
{
    private const int PrintedAtkBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkBonus) };

    public Black_Pendant()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
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
        typeof(Black_Pendant),
    };

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(3m);
}
