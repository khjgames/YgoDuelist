using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Display-only: this duel monster is marked as tribute material for the current turn.</summary>
public sealed class MarkedSacrificePower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "MARKED_SACRIFICE_POWER.title");

    public override LocString Description => new("powers", "MARKED_SACRIFICE_POWER.description");
}
