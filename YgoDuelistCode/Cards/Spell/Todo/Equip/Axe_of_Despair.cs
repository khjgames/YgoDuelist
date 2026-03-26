using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Axe_of_Despair : BaseEquipSpellCard
{
    private const int PrintedAtkBonus = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkBonus) };

    public Axe_of_Despair()
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
