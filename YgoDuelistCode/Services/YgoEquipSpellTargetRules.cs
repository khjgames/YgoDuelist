using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Gearfried_the_Iron_Knight"/> ignores equip spell race/restriction text.</summary>
public static class YgoEquipSpellTargetRules
{
    public static bool IsLegalEquipTarget(BaseEquipSpellCard equip, BaseMonsterCard target)
    {
        if (target is Gearfried_the_Iron_Knight)
            return true;
        return equip.CanEquipTo(target);
    }
}
