using MegaCrit.Sts2.Core.Entities.Powers;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Cosmetic-only buff powers (stance / face-down). Reuses stiff icon until dedicated art exists.</summary>
public abstract class YgoDuelistInfoPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayVfx => false;

    public override string CustomPackedIconPath => "stiff_power.png".PowerImagePath();

    public override string CustomBigIconPath => "stiff_power.png".BigPowerImagePath();
}
