using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Slate Warrior ATK/DEF stacks (+1 ATK and +1 DEF per stack).</summary>
public sealed class SlateWarriorPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SLATE_WARRIOR_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SLATE_WARRIOR_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-SLATE_WARRIOR_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "slate_warrior";
}
