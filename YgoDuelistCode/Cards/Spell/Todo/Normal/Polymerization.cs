using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Polymerization : FusionSpellCard
{
    public Polymerization()
        : base(
            cost: 1,
            rarity: CardRarity.Common,
            target: TargetType.Self,
            spellRace: DuelMonsterRace.SpellNormal,
            fusionTargetMonsterType: typeof(FusionMonsterCard),
            requiresPlayerFusionTargetSelection: false)
    {
    }

    protected override void OnUpgrade()
    {
    }
}
