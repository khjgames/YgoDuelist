using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class Curse_of_the_Masked_Beast : RitualSpellCard
{
    public Curse_of_the_Masked_Beast()
        : base(
            cost: 1,
            rarity: CardRarity.Rare,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(The_Masked_Beast),
            levelRequirement: 8,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] BundledCards => new[] { typeof(Curse_of_the_Masked_Beast), typeof(The_Masked_Beast) };
}
