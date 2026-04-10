using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Upgraded 7 Weapons on a duel monster; at <see cref="SevenWeaponsPower.StacksForAtkBonus"/> stacks,
/// <see cref="BaseMonsterCard.CalcDuelMonsterStats"/> adds <see cref="AtkBonusAtMaxStacks"/> ATK.
/// </summary>
public sealed class SevenWeaponsPlusPower : YgoDuelistPower
{
    public const int AtkBonusAtMaxStacks = 14;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SEVEN_WEAPONS_PLUS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SEVEN_WEAPONS_PLUS_POWER.description");

    protected override string? CardPortraitStemOverride => "the_hunter_with_7_weapons";

    public int GetDuelMonsterAtkBonusFromStacks() =>
        (int)Amount == SevenWeaponsPower.StacksForAtkBonus ? AtkBonusAtMaxStacks : 0;
}
