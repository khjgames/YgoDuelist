using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>UI-only marker on Machine duel monsters while <see cref="LimiterRemovalPower"/> is active.</summary>
public sealed class LimitRemovedPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-LIMIT_REMOVED_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-LIMIT_REMOVED_POWER.description");
}
