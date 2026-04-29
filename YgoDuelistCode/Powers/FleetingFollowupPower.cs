using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Next <see cref="Command.Command_Attack"/> from this duel monster costs 0 energy; removed after that attack or at end of your turn.
/// </summary>
public sealed class FleetingFollowupPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-FLEETING_FOLLOWUP_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FLEETING_FOLLOWUP_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-FLEETING_FOLLOWUP_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "black_luster_soldier_envoy_of_the_beginning";
}
