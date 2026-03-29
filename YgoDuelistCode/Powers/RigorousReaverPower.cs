using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Marker debuff from <see cref="Cards.Monster.Todo.Effect.Rigorous_Reaver"/>; Strength/Dexterity are applied separately (silent).</summary>
public sealed class RigorousReaverPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-RIGOROUS_REAVER_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-RIGOROUS_REAVER_POWER.description");
}
