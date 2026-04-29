using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Exodia Necross: +1 ATK per stack (gains printed <c>Mgc</c> stacks each of your turns).</summary>
public sealed class ExodiaNecrossAtkPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-EXODIA_NECROSS_ATK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-EXODIA_NECROSS_ATK_POWER.description");
}
