using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Upgraded consumable stacks; spend via monster-options <c>Activate Shackles +</c> to apply <see cref="ActiveShacklesPlusPower"/>.</summary>
public sealed class ConsumableShacklesPlusPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-CONSUMABLE_SHACKLES_PLUS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-CONSUMABLE_SHACKLES_PLUS_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-CONSUMABLE_SHACKLES_PLUS_POWER.smartDescription";

    public override string CustomPackedIconPath => "dark_shackles_power.png".PowerImagePath();

    public override string CustomBigIconPath => "dark_shackles_power.png".PowerImagePath();
}
