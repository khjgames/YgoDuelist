using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;

public sealed class Commencement_Dance : RitualSpellCard
{
    public Commencement_Dance()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Performance_of_Sword),
            levelRequirement: 6,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] BundledCards => new[] { typeof(Commencement_Dance), typeof(Performance_of_Sword) };
}
