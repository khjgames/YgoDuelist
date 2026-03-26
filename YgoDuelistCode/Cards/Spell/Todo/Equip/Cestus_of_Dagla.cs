using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

/// <summary>Cestus of Dagla — Spellcaster equip; restore HP when the equipped monster attacks.</summary>
public sealed class Cestus_of_Dagla : BaseEquipSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Cestus_of_Dagla()
        : base(1, CardRarity.Common, TargetType.Self)
    {
    }

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Spellcaster;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
