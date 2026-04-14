using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// While Enraged Battle Ox is face-up on your field, your Beast-Warrior monsters gain splinter on attacks.
/// </summary>
public sealed class EnragedBattleOxPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-ENRAGED_BATTLE_OX_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ENRAGED_BATTLE_OX_POWER.description");
}
