using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class Earth_Chant : RitualSpellCard
{
    public Earth_Chant()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(RitualMonsterCard),
            levelRequirement: 0,
            materialLevelCompare: RitualMaterialLevelCompare.Exact,
            ritualTargetAttributeFilter: DuelMonsterAttribute.Earth,
            useRitualTargetLevelAsMaterialRequirement: true)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] RelatedCards => RitualArchetypeMeta.NonRitualCardsReferencingRitualInLocalization;
}
