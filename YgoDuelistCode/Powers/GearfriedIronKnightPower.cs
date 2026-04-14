using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>+1 ATK and +1 DEF per stack when Gearfried destroys an Equip Spell equipped to him.</summary>
public sealed class GearfriedIronKnightPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-GEARFRIED_IRON_KNIGHT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-GEARFRIED_IRON_KNIGHT_POWER.description");

    protected override string? CardPortraitStemOverride => "gearfried_the_iron_knight";
}
