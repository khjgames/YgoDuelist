using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;

public sealed class Garma_Sword_Oath : RitualSpellCard
{
    public Garma_Sword_Oath()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Garma_Sword),
            levelRequirement: 7,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Ritual | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Garma_Sword_Oath), typeof(Garma_Sword) };

}
