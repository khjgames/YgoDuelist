using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Stack count: each stack grants +1 Max HP to your duel monsters (HP bonus tracked in <see cref="Services.YgoDuelistPassivePowerState"/> when the card is played).
/// </summary>
public sealed class FortifiedBeastsPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-FORTIFIED_BEASTS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FORTIFIED_BEASTS_POWER.description");
}
