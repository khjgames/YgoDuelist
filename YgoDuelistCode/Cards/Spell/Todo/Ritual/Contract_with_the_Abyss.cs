using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class Contract_with_the_Abyss : RitualSpellCard
{
    public Contract_with_the_Abyss()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(RitualMonsterCard),
            levelRequirement: 0,
            materialLevelCompare: RitualMaterialLevelCompare.Exact,
            ritualTargetAttributeFilter: DuelMonsterAttribute.Dark,
            useRitualTargetLevelAsMaterialRequirement: true)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] RelatedCards => RitualArchetypeMeta.NonRitualCardsReferencingRitualInLocalization;
}
