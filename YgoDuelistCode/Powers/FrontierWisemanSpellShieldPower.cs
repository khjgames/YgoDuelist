using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Spell/Trap destruction protection granted by <see cref="Cards.Monster.Done.Effect.Frontier_Wiseman"/> to Warrior duel monsters.</summary>
public sealed class FrontierWisemanSpellShieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-MAGIC_PROTECTION_KEYWORD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FRONTIER_WISEMAN_SPELL_SHIELD_POWER.description");
}
