using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class Javelin_Beetle_Pact : RitualSpellCard
{
    public Javelin_Beetle_Pact()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Javelin_Beetle),
            levelRequirement: 8,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] BundledCards => new[] { typeof(Javelin_Beetle_Pact), typeof(Javelin_Beetle) };

    public override Type[] RelatedCards =>
        RitualArchetypeMeta.RelatedCardsForPairedRitual(typeof(Javelin_Beetle_Pact), typeof(Javelin_Beetle));

}
