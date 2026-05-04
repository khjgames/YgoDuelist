using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;

public sealed class Contract_with_the_Dark_Master : RitualSpellCard
{
    public Contract_with_the_Dark_Master()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellRitual,
            ritualTargetMonsterType: typeof(Dark_Master_Zorc),
            levelRequirement: 8,
            materialLevelCompare: RitualMaterialLevelCompare.AtLeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Ritual | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Contract_with_the_Dark_Master), typeof(Dark_Master_Zorc) };
}
