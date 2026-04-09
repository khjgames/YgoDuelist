using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Your next Spell activation costs 1 less Energy (consumed when you play a Spell).</summary>
public sealed class DeSpellNextSpellDiscountPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-DE_SPELL_NEXT_SPELL_DISCOUNT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DE_SPELL_NEXT_SPELL_DISCOUNT_POWER.description");
}
