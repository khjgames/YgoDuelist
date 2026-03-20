using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

public sealed class AttackPositionPower : YgoDuelistInfoPower
{
    public override LocString Title => new("powers", "ATTACK_POSITION_POWER.title");

    public override LocString Description => new("powers", "ATTACK_POSITION_POWER.description");
}
