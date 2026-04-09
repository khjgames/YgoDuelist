using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Every N duel monster summons, gain 1 Conduit (star). N is 4, or 3 if upgraded.
/// </summary>
public sealed class ChainSummoningPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-CHAIN_SUMMONING_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-CHAIN_SUMMONING_POWER.description");
}
