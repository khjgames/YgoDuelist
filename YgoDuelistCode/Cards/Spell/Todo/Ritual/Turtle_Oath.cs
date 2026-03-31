using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class Turtle_Oath : RitualSpellCard
{
    public Turtle_Oath()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Crab_Turtle),
            levelRequirement: 8,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] BundledCards => new[] { typeof(Turtle_Oath), typeof(Crab_Turtle) };

    public override Type[] RelatedCards =>
        RitualArchetypeMeta.RelatedCardsForPairedRitual(typeof(Turtle_Oath), typeof(Crab_Turtle));

    protected override void OnUpgrade()
    {
        ExecuteSpellUpgradePlaceholder();
    }

    private void ExecuteSpellUpgradePlaceholder()
    {
    }
}
