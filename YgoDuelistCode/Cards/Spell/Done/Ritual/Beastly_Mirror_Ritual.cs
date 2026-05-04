using YgoDuelist.YgoDuelistCode.Cards;
using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;

public sealed class Beastly_Mirror_Ritual : RitualSpellCard
{
    public Beastly_Mirror_Ritual()
        : base(
            cost: 1,
            rarity: CardRarity.Uncommon,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Fiend_S_Mirror),
            levelRequirement: 6,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Ritual | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Beastly_Mirror_Ritual), typeof(Fiend_S_Mirror) };
}
