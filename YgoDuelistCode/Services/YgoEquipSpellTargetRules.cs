using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Uses <see cref="BaseMonsterCard.IgnoresEquipSpellRaceRestrictions"/> (e.g. Gearfried).</summary>
public static class YgoEquipSpellTargetRules
{
    public static bool IsLegalEquipTarget(BaseEquipSpellCard equip, BaseMonsterCard target)
    {
        if (target.IgnoresEquipSpellRaceRestrictions)
            return true;
        return equip.CanEquipTo(target);
    }
}
