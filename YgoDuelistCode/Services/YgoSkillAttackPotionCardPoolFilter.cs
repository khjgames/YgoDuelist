using System.Diagnostics;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="SkillPotion"/> / <see cref="AttackPotion"/> use <see cref="MegaCrit.Sts2.Core.Factories.CardFactory.GetDistinctForCombat"/>.
/// YGO pools should omit unfinished-tagged cards, extra-deck monsters, rituals, and monsters that cannot be normal summoned.
/// </summary>
public static class YgoSkillAttackPotionCardPoolFilter
{
    public static IEnumerable<CardModel> FilterIfSkillOrAttackPotion(Player player, IEnumerable<CardModel> cards)
    {
        if (player?.Character is not Character.YgoDuelist)
            return cards;
        if (!CallerIsSkillOrAttackPotionOnUse())
            return cards;
        return cards.Where(PassesYgoSkillAttackPotionRules);
    }

    private static bool PassesYgoSkillAttackPotionRules(CardModel c)
    {
        if (c is not YgoDuelistCard ygo)
            return true;
        if (ygo.PackTags == YgoCardPackTags.None)
            return false;
        if (c is IYgoCard iy && (iy.YgoCardType == YgoCardType.FusionMonster || iy.YgoCardType == YgoCardType.RitualMonster))
            return false;
        if (c is BaseMonsterCard bm && !bm.CanSummonDuelMonster)
            return false;
        return true;
    }

    private static bool CallerIsSkillOrAttackPotionOnUse()
    {
        foreach (StackFrame? frame in new StackTrace(skipFrames: 1, fNeedFileInfo: false).GetFrames() ?? [])
        {
            System.Reflection.MethodBase? method = frame?.GetMethod();
            if (method == null)
                continue;
            for (Type? t = method.DeclaringType; t != null; t = t.DeclaringType)
            {
                if (t == typeof(SkillPotion) || t == typeof(AttackPotion))
                    return true;
            }
        }

        return false;
    }
}
