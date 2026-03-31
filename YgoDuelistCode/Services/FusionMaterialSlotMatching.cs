using System;
using System.Collections.Generic;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Fusion multiset matching for <see cref="FusionMaterialSlot"/> recipes (named, requirement, substitute rules).</summary>
public static class FusionMaterialSlotMatching
{
    /// <summary>At most one <see cref="IFusionMaterialSubstitute"/> in the picked set; applies only to named paths, never to requirement paths.</summary>
    public static bool MaterialsMatchSlots(IReadOnlyList<FusionMaterialSlot> slots, List<BaseMonsterCard> picked)
    {
        if (slots.Count != picked.Count)
            return false;
        int substituteCount = picked.Count(IsFusionSubstitute);
        if (substituteCount > 1)
            return false;
        return TryMatchSlots(slots, picked, 0, new bool[picked.Count], substituteUsed: false);
    }

    private static bool TryMatchSlots(
        IReadOnlyList<FusionMaterialSlot> slots,
        List<BaseMonsterCard> pick,
        int slotIndex,
        bool[] used,
        bool substituteUsed)
    {
        if (slotIndex >= slots.Count)
            return true;
        FusionMaterialSlot slot = slots[slotIndex];
        for (int j = 0; j < pick.Count; j++)
        {
            if (used[j])
                continue;
            BaseMonsterCard card = pick[j];
            bool sub = substituteUsed;
            if (MonsterMatchesSlot(slot, card, ref sub))
            {
                used[j] = true;
                if (TryMatchSlots(slots, pick, slotIndex + 1, used, sub))
                    return true;
                used[j] = false;
            }
        }

        return false;
    }

    private static bool MonsterMatchesSlot(FusionMaterialSlot slot, BaseMonsterCard card, ref bool substituteUsed)
    {
        return slot.Mode switch
        {
            FusionMaterialSlotMode.NamedOnly => MatchesNamedOnly(slot, card, ref substituteUsed),
            FusionMaterialSlotMode.RequirementOnly => MatchesRequirementOnly(slot.Req, card),
            FusionMaterialSlotMode.NamedOrRequirement => MatchesNamedOrRequirement(slot, card, ref substituteUsed),
            _ => false
        };
    }

    private static bool MatchesNamedOnly(FusionMaterialSlot slot, BaseMonsterCard card, ref bool substituteUsed)
    {
        Type? n = slot.NamedType;
        if (n != null && n.IsInstanceOfType(card))
            return true;
        if (!substituteUsed && IsFusionSubstitute(card))
        {
            substituteUsed = true;
            return true;
        }

        return false;
    }

    private static bool MatchesRequirementOnly(FusionMaterialRequirements req, BaseMonsterCard card)
    {
        if (IsFusionSubstitute(card))
            return false;
        return SatisfiesRequirements(card, req);
    }

    private static bool MatchesNamedOrRequirement(FusionMaterialSlot slot, BaseMonsterCard card, ref bool substituteUsed)
    {
        Type? n = slot.NamedType;
        if (n != null && n.IsInstanceOfType(card))
            return true;
        if (!substituteUsed && IsFusionSubstitute(card))
        {
            substituteUsed = true;
            return true;
        }

        if (IsFusionSubstitute(card))
            return false;
        return SatisfiesRequirements(card, slot.Req);
    }

    public static bool SatisfiesRequirements(BaseMonsterCard card, FusionMaterialRequirements r)
    {
        FusionMaterialRequirementFilterMask m = r.FilterMask;
        if (m == FusionMaterialRequirementFilterMask.None)
            return false;

        if (m.HasFlag(FusionMaterialRequirementFilterMask.Level))
        {
            int lv = card.DuelMonsterLevel;
            if (lv < r.LevelMin || lv > r.LevelMax)
                return false;
        }

        if (m.HasFlag(FusionMaterialRequirementFilterMask.Attribute))
        {
            if (r.Attributes == DuelMonsterAttributeMask.None)
                return false;
            DuelMonsterAttributeMask bit = card.DuelMonsterAttribute.ToMask();
            if ((r.Attributes & bit) == 0)
                return false;
        }

        if (m.HasFlag(FusionMaterialRequirementFilterMask.Atk))
        {
            int atk = card.BaseAtk;
            if (atk < r.AtkMin || atk > r.AtkMax)
                return false;
        }

        if (m.HasFlag(FusionMaterialRequirementFilterMask.Def))
        {
            int def = card.BaseDef;
            if (def < r.DefMin || def > r.DefMax)
                return false;
        }

        if (m.HasFlag(FusionMaterialRequirementFilterMask.Race))
        {
            if (r.Races.Bits == 0)
                return false;
            if (!r.Races.Contains(card.DuelMonsterRace))
                return false;
        }

        return true;
    }

    private static bool IsFusionSubstitute(BaseMonsterCard card) =>
        card is IFusionMaterialSubstitute substitute && substitute.CanSubstituteAsFusionMaterial;
}
