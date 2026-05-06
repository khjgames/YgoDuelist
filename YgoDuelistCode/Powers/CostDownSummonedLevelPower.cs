using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Temporary level-down applied to duel monster pets that were summoned from hand while Cost Down is active.
/// Amount is the total level reduction (2 per Cost Down stack).
/// </summary>
public sealed class CostDownSummonedLevelPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-COST_DOWN_SUMMONED_LEVEL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-COST_DOWN_SUMMONED_LEVEL_POWER.description");
}
