using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary ATK/DEF from Spirit Ryu; cleared at end of player turn.</summary>
public sealed class SpiritRyuTempAtkDefPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SPIRIT_RYU_TEMP_ATK_DEF_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPIRIT_RYU_TEMP_ATK_DEF_POWER.description");

    protected override string? CardPortraitStemOverride => "spirit_ryu";
}
