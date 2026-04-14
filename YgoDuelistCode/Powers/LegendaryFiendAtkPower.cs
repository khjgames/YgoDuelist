using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Legendary Fiend ATK stacks (+1 ATK per stack on the duel monster).
/// </summary>
public sealed class LegendaryFiendAtkPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-LEGENDARY_FIEND_ATK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-LEGENDARY_FIEND_ATK_POWER.description");
}
