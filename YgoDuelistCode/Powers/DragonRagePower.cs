using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// While Dragon's Rage is face-up in your Spell/Trap zone, your Dragon monsters gain splinter on attacks.
/// </summary>
public sealed class DragonRagePower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-DRAGON_RAGE_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DRAGON_RAGE_POWER.description");
}
