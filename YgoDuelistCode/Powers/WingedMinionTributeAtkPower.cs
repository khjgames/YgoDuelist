using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// ATK bonus from Winged Minion's activated effect; persists until this duel monster leaves the field.
/// </summary>
public sealed class WingedMinionTributeAtkPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-WINGED_MINION_TRIBUTE_ATK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-WINGED_MINION_TRIBUTE_ATK_POWER.description");

    public override string CustomPackedIconPath => "rush_recklessly_power.png".PowerImagePath();

    public override string CustomBigIconPath => "rush_recklessly_power.png".BigPowerImagePath();
}
