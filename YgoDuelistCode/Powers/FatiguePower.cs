using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

public sealed class FatiguePower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "FATIGUE_POWER.title");

    public override LocString Description => new("powers", "FATIGUE_POWER.description");
}
