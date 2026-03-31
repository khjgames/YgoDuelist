using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;

public sealed class War_Lion_Ritual : RitualSpellCard
{
    public War_Lion_Ritual()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Super_War_Lion),
            levelRequirement: 7,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual;

    public override Type[] BundledCards => new[] { typeof(War_Lion_Ritual), typeof(Super_War_Lion) };

    public override Type[] RelatedCards =>
        RitualArchetypeMeta.RelatedCardsForPairedRitual(typeof(War_Lion_Ritual), typeof(Super_War_Lion));

    protected override void OnUpgrade()
    {
        ExecuteSpellUpgradePlaceholder();
    }

    private void ExecuteSpellUpgradePlaceholder()
    {
    }
}
