using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Display-only marker: cannot be destroyed or targeted by monster destruction (see <see cref="Services.YgoDuelMonsterDestructionRules"/>).</summary>
public sealed class MonsterProtectionKeywordPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-MONSTER_PROTECTION_KEYWORD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-MONSTER_PROTECTION_KEYWORD_POWER.description");
}
