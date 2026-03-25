using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Tracks 1–7 progress for The Hunter with 7 Weapons.</summary>
public sealed class SevenWeaponsPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SEVEN_WEAPONS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SEVEN_WEAPONS_POWER.description");
}
