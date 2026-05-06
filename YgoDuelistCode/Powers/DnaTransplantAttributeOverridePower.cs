using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

public sealed class DnaTransplantAttributeOverridePower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DNA_TRANSPLANT_ATTRIBUTE_OVERRIDE_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DNA_TRANSPLANT_ATTRIBUTE_OVERRIDE_POWER.description");
}
