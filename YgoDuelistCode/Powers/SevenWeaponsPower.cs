using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Counter on a duel monster; at <see cref="StacksForAtkBonus"/> stacks, <see cref="BaseMonsterCard.CalcDuelMonsterStats"/>
/// adds <see cref="AtkBonusAtMaxStacks"/> ATK (any field monster whose pet has this power).
/// </summary>
public sealed class SevenWeaponsPower : YgoDuelistPower
{
    /// <summary>Stack count at which <see cref="AtkBonusAtMaxStacks"/> applies.</summary>
    public const int StacksForAtkBonus = 7;

    /// <summary>Flat ATK while stacks equal <see cref="StacksForAtkBonus"/>.</summary>
    public const int AtkBonusAtMaxStacks = 10;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SEVEN_WEAPONS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SEVEN_WEAPONS_POWER.description");

    protected override string? CardPortraitStemOverride => "the_hunter_with_7_weapons";

    /// <summary>ATK contributed in <see cref="BaseMonsterCard.CalcDuelMonsterStats"/> when this power is on the field monster’s pet.</summary>
    public int GetDuelMonsterAtkBonusFromStacks() =>
        (int)Amount == StacksForAtkBonus ? AtkBonusAtMaxStacks : 0;
}
