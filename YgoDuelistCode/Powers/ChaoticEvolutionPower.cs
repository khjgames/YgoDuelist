using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Chaos Sorcerer / Chaos Daedalus: +1 ATK and +1 DEF per stack (same stacking model as <see cref="NecroticEvolutionPower"/>).
/// </summary>
public sealed class ChaoticEvolutionPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-CHAOTIC_EVOLUTION_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-CHAOTIC_EVOLUTION_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-CHAOTIC_EVOLUTION_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "chaos_sorcerer";
}
