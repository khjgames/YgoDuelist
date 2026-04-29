using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Display-only marker: cannot be destroyed or targeted by spell/trap destruction (see <see cref="Services.YgoDuelMonsterDestructionRules"/>).</summary>
public sealed class MagicProtectionKeywordPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-MAGIC_PROTECTION_KEYWORD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-MAGIC_PROTECTION_KEYWORD_POWER.description");
}
