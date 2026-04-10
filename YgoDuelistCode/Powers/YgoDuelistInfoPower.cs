using MegaCrit.Sts2.Core.Entities.Powers;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Cosmetic-only buff powers (stance / face-down). Icons use <see cref="YgoDuelistPower"/> resolution + preload.</summary>
public abstract class YgoDuelistInfoPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayVfx => false;
}
