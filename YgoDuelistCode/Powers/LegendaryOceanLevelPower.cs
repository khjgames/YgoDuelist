using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Active while A Legendary Ocean is face-up and affecting this pet.
/// Amount is the total level reduction from all active copies.
/// </summary>
public sealed class LegendaryOceanLevelPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-LEGENDARY_OCEAN_LEVEL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-LEGENDARY_OCEAN_LEVEL_POWER.description");
}
