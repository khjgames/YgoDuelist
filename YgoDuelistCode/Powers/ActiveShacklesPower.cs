using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary Strength loss on an enemy (Dark Shackles–style); each activation adds <see cref="StrengthLossPerApply"/> to this debuff’s total for the turn.</summary>
public sealed class ActiveShacklesPower : TemporaryStrengthPower, ICustomPower
{
    public const int StrengthLossPerApply = 5;

    public override AbstractModel OriginModel => ModelDb.Card<DarkShackles>();

    string? ICustomPower.CustomPackedIconPath => "dark_shackles_power.png".PowerImagePath();

    string? ICustomPower.CustomBigIconPath => "dark_shackles_power.png".PowerImagePath();

    string? ICustomPower.CustomBigBetaIconPath => null;

    public override LocString Title => new("powers", "YGODUELIST-ACTIVE_SHACKLES_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ACTIVE_SHACKLES_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-ACTIVE_SHACKLES_POWER.smartDescription";

    protected override bool IsPositive => false;
}
