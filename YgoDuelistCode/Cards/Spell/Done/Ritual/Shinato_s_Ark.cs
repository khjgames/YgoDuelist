using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;

public sealed class Shinato_S_Ark : RitualSpellCard
{
    public Shinato_S_Ark()
        : base(
            cost: 1,
            rarity: CardRarity.Rare,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Shinato_King_of_a_Higher_Plane),
            levelRequirement: 8,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Ritual | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Shinato_S_Ark), typeof(Shinato_King_of_a_Higher_Plane) };

}
