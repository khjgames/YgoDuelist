using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Polymerization : FusionSpellCard
{
    public Polymerization()
        : base(
            cost: 1,
            rarity: CardRarity.Uncommon,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellNormal,
            fusionTargetMonsterType: typeof(FusionMonsterCard),
            requiresPlayerFusionTargetSelection: false)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Fusion | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Polymerization),
        typeof(Fusion_Sage),
        typeof(Fusion_Gate),
    };

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
