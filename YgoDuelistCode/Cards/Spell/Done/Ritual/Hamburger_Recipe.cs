using YgoDuelist.YgoDuelistCode.Cards;
using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;

public sealed class Hamburger_Recipe : RitualSpellCard
{
    public Hamburger_Recipe()
        : base(
            cost: 1,
            rarity: CardRarity.Uncommon,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Hungry_Burger),
            levelRequirement: 6,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Ritual;

    public override Type[] BundledCards => new[] { typeof(Hamburger_Recipe), typeof(Hungry_Burger) };
}
