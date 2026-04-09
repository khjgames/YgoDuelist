using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Your next Trap activation costs 1 less Energy (consumed when you play a Trap).</summary>
public sealed class FakeTrapNextTrapDiscountPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-FAKE_TRAP_NEXT_TRAP_DISCOUNT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FAKE_TRAP_NEXT_TRAP_DISCOUNT_POWER.description");
}
