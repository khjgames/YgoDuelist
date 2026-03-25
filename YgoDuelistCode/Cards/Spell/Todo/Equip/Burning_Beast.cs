using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

/// <summary>
/// Burning Beast — equipped monster's attacks apply 1 Weak and 1 Vulnerable.
/// (Resolved in <see cref="Relics.GraveyardRelic.AfterAttack"/>.)
/// </summary>
public sealed class Burning_Beast : BaseEquipSpellCard
{
    public Burning_Beast()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    protected override void OnUpgrade()
    {
        // No direct stat change; weak/vulnerable application is handled externally.
    }
}

