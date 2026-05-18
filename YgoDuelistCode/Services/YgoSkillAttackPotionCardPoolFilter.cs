using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.TrapMonster;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="SkillPotion"/> / <see cref="AttackPotion"/> use <see cref="MegaCrit.Sts2.Core.Factories.CardFactory.GetDistinctForCombat"/>.
/// YGO pools should omit unfinished-tagged cards, extra-deck monsters, rituals, and monsters that cannot be normal summoned.
/// </summary>
public static class YgoSkillAttackPotionCardPoolFilter
{
    /// <summary>
    /// Catalog templates must not evaluate <see cref="AbstractMonsterCard.CanSummonDuelMonster"/> when it reads
    /// <see cref="CardModel.Owner"/> (e.g. <see cref="Cave_Dragon"/>). These types hard-block normal summon without Owner.
    /// </summary>
    private static readonly HashSet<Type> CatalogNormalSummonBlockedForPotion =
    [
        typeof(YgoTokenNormalMonster),
        typeof(YgoTokenEffectMonster),
        typeof(Aqua_Spirit),
        typeof(Berserk_Dragon),
        typeof(Black_Luster_Soldier_Envoy_of_the_Beginning),
        typeof(Chaos_Daedalus),
        typeof(Chaos_Emperor_Dragon_Envoy_of_the_End),
        typeof(Chaos_Sorcerer),
        typeof(Dark_Necrofear),
        typeof(Dark_Sage),
        typeof(Endless_Decay),
        typeof(Embodiment_of_Apophis_Trap_Monster),
        typeof(Fenrir),
        typeof(Fushioh_Richie),
        typeof(Garuda_the_Wind_Spirit),
        typeof(Lightray_Daedalus),
        typeof(Metal_Reflect_Slime_Trap_Monster),
        typeof(Mirage_Knight),
        typeof(Ocean_Dragon_Lord_Neo_Daedalus),
        typeof(Silpheed),
        typeof(Soul_of_Purity_and_Light),
        typeof(Spirit_of_Flames),
        typeof(Spirit_of_the_Pharaoh),
        typeof(The_First_Monarch_Trap_Monster),
        typeof(Valkyrion_the_Magna_Warrior),
        typeof(Wall_Shadow),
    ];

    /// <summary>Ygo Duelist combat card grid from skill or attack potion (vanilla <c>OnUse</c> stack).</summary>
    public static bool IsYgoSkillOrAttackPotionContext(Player? player) =>
        player?.Character is Character.YgoDuelist && CallerIsSkillOrAttackPotionOnUse();

    public static IEnumerable<CardModel> FilterIfSkillOrAttackPotion(Player player, IEnumerable<CardModel> cards)
    {
        if (!IsYgoSkillOrAttackPotionContext(player))
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
        if (c is BaseMonsterCard bm && !PassesPotionSummonGate(bm))
            return false;
        return true;
    }

    private static bool PassesPotionSummonGate(BaseMonsterCard bm)
    {
        if (bm.IsCanonical)
        {
            Type t = bm.GetType();
            foreach (Type blocked in CatalogNormalSummonBlockedForPotion)
            {
                if (blocked.IsAssignableFrom(t))
                    return false;
            }

            return true;
        }

        return bm.CanSummonDuelMonster;
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
