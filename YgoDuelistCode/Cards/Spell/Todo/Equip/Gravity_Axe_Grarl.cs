using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

/// <summary>TCG "Gravity Axe - Grarl": +500 ATK (no piercing).</summary>
public sealed class Gravity_Axe_Grarl : BaseEquipSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 5m) };

    public Gravity_Axe_Grarl()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

    protected override void OnUpgrade()
    {
        int printed = (int)DynamicVars["Mgc"].BaseValue;
        DynamicVars["Mgc"].BaseValue = printed + YgoStatUpgradeScaling.GetSpellTrapStatBonusUpgradeDelta(printed);
    }
}
