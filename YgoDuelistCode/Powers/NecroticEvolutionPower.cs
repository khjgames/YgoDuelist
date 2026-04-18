using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Zombie-Type ATK/DEF bonus: +1 ATK and +1 DEF per stack (from Necrotic Ritual pulses).
/// </summary>
public sealed class NecroticEvolutionPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-NECROTIC_EVOLUTION_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-NECROTIC_EVOLUTION_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-NECROTIC_EVOLUTION_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "castle_of_dark_illusions";
}
