using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// While active on this duel monster pet, <see cref="Cards.Monster.Done.Effect.Panther_Warrior"/> cannot use Command Attack.
/// </summary>
public sealed class PantherWarriorRefusalPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-PANTHER_WARRIOR_REFUSAL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-PANTHER_WARRIOR_REFUSAL_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-PANTHER_WARRIOR_REFUSAL_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "panther_warrior";
}
