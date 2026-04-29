using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Archfiend of Gilfer: +1 ATK and +1 DEF per stack on the duel monster pet.</summary>
public sealed class GilferPowerPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-GILFER_POWER_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-GILFER_POWER_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-GILFER_POWER_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "archfiend_of_gilfer";
}
