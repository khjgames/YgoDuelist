using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Upgraded Active Shackles: <see cref="StrengthLossPerApply"/> temporary Strength per stack applied this turn.</summary>
public sealed class ActiveShacklesPlusPower : TemporaryStrengthPower, ICustomPower
{
    public const int StrengthLossPerApply = 8;

    public override AbstractModel OriginModel => ModelDb.Card<DarkShackles>();

    string? ICustomPower.CustomPackedIconPath => "dark_shackles_power.png".PowerImagePath();

    string? ICustomPower.CustomBigIconPath => "dark_shackles_power.png".PowerImagePath();

    string? ICustomPower.CustomBigBetaIconPath => null;

    public override LocString Title => new("powers", "YGODUELIST-ACTIVE_SHACKLES_PLUS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ACTIVE_SHACKLES_PLUS_POWER.description");

    protected override bool IsPositive => false;
}
