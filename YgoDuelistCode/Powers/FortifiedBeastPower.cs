using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Stack count: this duel monster has +1 Max HP per stack.</summary>
public sealed class FortifiedBeastPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-FORTIFIED_BEAST_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FORTIFIED_BEAST_POWER.description");

    protected override string? CardPortraitStemOverride => "big_shield_gardna";
}
