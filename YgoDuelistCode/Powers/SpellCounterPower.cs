using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Player-visible spell counter stock for YGO effects (traps, fields, etc.).
/// </summary>
public sealed class SpellCounterPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SPELL_COUNTER_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPELL_COUNTER_POWER.description");
}
