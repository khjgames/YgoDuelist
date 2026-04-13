using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// +1 ATK per stack on this duel monster from Insect Princess execute kills; cleared when the monster leaves the field.
/// </summary>
public sealed class InsectPrincessExecuteAtkPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-INSECT_PRINCESS_EXECUTE_ATK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-INSECT_PRINCESS_EXECUTE_ATK_POWER.description");
}
