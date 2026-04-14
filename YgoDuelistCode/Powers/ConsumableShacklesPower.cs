using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Stacks on a duel monster; spend via monster-options <c>Activate Shackles</c> to apply <see cref="ActiveShacklesPower"/> to an enemy.</summary>
public sealed class ConsumableShacklesPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-CONSUMABLE_SHACKLES_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-CONSUMABLE_SHACKLES_POWER.description");

    public override string CustomPackedIconPath => "dark_shackles_power.png".PowerImagePath();

    public override string CustomBigIconPath => "dark_shackles_power.png".PowerImagePath();
}
