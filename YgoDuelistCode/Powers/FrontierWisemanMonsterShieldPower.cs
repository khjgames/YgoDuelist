using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Monster-effect destruction protection granted by upgraded <see cref="Cards.Monster.Done.Effect.Frontier_Wiseman"/> to Warrior duel monsters.</summary>
public sealed class FrontierWisemanMonsterShieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-MONSTER_PROTECTION_KEYWORD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FRONTIER_WISEMAN_MONSTER_SHIELD_POWER.description");
}
