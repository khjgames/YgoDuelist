using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class RitualSummonSelection
{
    private static readonly AsyncLocal<bool> CompletingRitualSpellPlay = new();

    private static readonly LocString PickTargetPrompt = new LocString("combat_messages", "RITUAL_SUMMON_PICK_TARGET");

    /// <summary>While true, <see cref="RitualSpellCard.IsPlayable"/> skips ritual feasibility (post-grid play resolution).</summary>
    public static bool IsCompletingRitualSpellPlay => CompletingRitualSpellPlay.Value;

    public static void BeginCompletingRitualSpellPlay() => CompletingRitualSpellPlay.Value = true;

    public static void EndCompletingRitualSpellPlay() => CompletingRitualSpellPlay.Value = false;

    public static bool HasRitualTargetInHand(Player player, RitualSpellCard spell)
    {
        return GetRitualTargetsInHand(player, spell).Count > 0;
    }

    /// <summary>
    /// True if some ritual target in hand can be summoned: non-empty material subset from hand/field
    /// (excluding spell and that target) whose total level matches the spell rule and zone capacity after killing field materials.
    /// </summary>
    public static bool HasFeasibleRitualPlay(Player player, RitualSpellCard spell)
    {
        foreach (RitualMonsterCard target in GetRitualTargetsInHand(player, spell))
        {
            if (HasFeasibleMaterialsForTarget(player, spell, target))
                return true;
        }

        return false;
    }

    private static bool HasFeasibleMaterialsForTarget(Player player, RitualSpellCard spell, RitualMonsterCard ritualTarget)
    {
        List<BaseMonsterCard> mats = BuildMaterialCandidates(player, spell, ritualTarget);
        int req = GetEffectiveMaterialLevelRequirement(spell, ritualTarget);
        RitualMaterialLevelCompare mode = GetEffectiveMaterialLevelCompare(spell);
        return HasFeasibleMaterialSubset(player, mats, req, mode, spell.ExactMaterialCardCount);
    }

    private static int GetEffectiveMaterialLevelRequirement(RitualSpellCard spell, RitualMonsterCard chosenTarget)
    {
        return spell.UseRitualTargetLevelAsMaterialRequirement ? chosenTarget.GetEffectiveDuelMonsterLevel() : spell.LevelRequirement;
    }

    private static RitualMaterialLevelCompare GetEffectiveMaterialLevelCompare(RitualSpellCard spell)
    {
        return spell.UseRitualTargetLevelAsMaterialRequirement ? RitualMaterialLevelCompare.Exact : spell.MaterialLevelCompare;
    }

    /// <summary>
    /// Subset DP over (level sum, field-tribute count); requires at least one material (matches confirm UI).
    /// </summary>
    private static bool HasFeasibleMaterialSubset(
        Player player,
        List<BaseMonsterCard> mats,
        int requirement,
        RitualMaterialLevelCompare mode,
        int? exactMaterialCardCount)
    {
        if (mats.Count == 0)
            return false;

        if (exactMaterialCardCount == 1)
        {
            foreach (BaseMonsterCard m in mats)
            {
                int lv = m.GetEffectiveDuelMonsterLevel();
                if (!LevelSumValid(lv, requirement, mode))
                    continue;
                int fc = TributeSummonSelection.ResolvePetForFieldCard(player, m) != null ? 1 : 0;
                if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, fc))
                    continue;
                return true;
            }

            return false;
        }

        var states = new HashSet<(int Sum, int FieldTributes)> { (0, 0) };

        foreach (BaseMonsterCard m in mats)
        {
            int lv = m.GetEffectiveDuelMonsterLevel();
            int fc = TributeSummonSelection.ResolvePetForFieldCard(player, m) != null ? 1 : 0;

            var next = new HashSet<(int Sum, int FieldTributes)>(states);
            foreach ((int s, int f) in states)
            {
                int ns = s + lv;
                int nf = f + fc;
                next.Add((ns, nf));
            }

            states = next;
        }

        foreach ((int sum, int fieldTributes) in states)
        {
            if (sum <= 0)
                continue;
            if (!LevelSumValid(sum, requirement, mode))
                continue;
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, fieldTributes))
                continue;
            return true;
        }

        return false;
    }

    public static List<RitualMonsterCard> GetRitualTargetsInHand(Player player, RitualSpellCard spell)
    {
        var list = new List<RitualMonsterCard>();
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return list;

        Type ritualTargetMonsterType = spell.RitualTargetMonsterType;
        DuelMonsterAttribute? attrFilter = spell.RitualTargetAttributeFilter;

        foreach (CardModel c in hand.Cards)
        {
            if (ReferenceEquals(c, spell))
                continue;
            if (c is not RitualMonsterCard rm || !ritualTargetMonsterType.IsInstanceOfType(rm))
                continue;
            if (attrFilter is { } a && rm.DuelMonsterAttribute != a)
                continue;
            list.Add(rm);
        }

        return list;
    }

    /// <summary>Field tribute sources plus hand monsters, excluding the spell and chosen ritual target.</summary>
    public static List<BaseMonsterCard> BuildMaterialCandidates(Player player, CardModel spellCard, RitualMonsterCard chosenTarget)
    {
        var list = new List<BaseMonsterCard>();
        foreach (BaseMonsterCard field in TributeSummonSelection.BuildTributeCandidateCards(player))
        {
            if (!ReferenceEquals(field, chosenTarget))
                list.Add(field);
        }

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null)
        {
            foreach (CardModel c in hand.Cards)
            {
                if (ReferenceEquals(c, spellCard) || ReferenceEquals(c, chosenTarget))
                    continue;
                if (c is BaseMonsterCard bm)
                    list.Add(bm);
            }
        }

        return list;
    }

    public static bool LevelSumValid(int sum, int requirement, RitualMaterialLevelCompare mode)
    {
        return mode == RitualMaterialLevelCompare.AtLeast ? sum >= requirement : sum == requirement;
    }

    /// <summary>Grids only; stores <see cref="RitualSpellPlayPayload"/> for the spell's <see cref="BaseSpellCard.OnPlay"/>.</summary>
    public static async Task<bool> TrySelectRitualResolutionAsync(Player player, RitualSpellCard spell)
    {
        var ctx = new BlockingPlayerChoiceContext();

        List<RitualMonsterCard> targets = GetRitualTargetsInHand(player, spell);
        if (targets.Count == 0)
            return false;

        bool needRitualTargetGrid = spell.RequiresPlayerRitualTargetSelection || targets.Count > 1;

        RitualMonsterCard ritualCard;
        if (needRitualTargetGrid)
        {
            var targetPrefs = new CardSelectorPrefs(PickTargetPrompt, 1, 1) { Cancelable = true };

            IEnumerable<CardModel> targetPick;
            try
            {
                targetPick = await CardSelectCmd.FromSimpleGrid(ctx, targets, player, targetPrefs);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            RitualMonsterCard? pickedTarget = targetPick.OfType<RitualMonsterCard>().FirstOrDefault();
            if (pickedTarget == null || !spell.RitualTargetMonsterType.IsInstanceOfType(pickedTarget))
                return false;
            if (spell.RitualTargetAttributeFilter is { } fa && pickedTarget.DuelMonsterAttribute != fa)
                return false;
            ritualCard = pickedTarget;
        }
        else
        {
            ritualCard = targets[0];
        }

        List<BaseMonsterCard> materials = BuildMaterialCandidates(player, spell, ritualCard);
        if (!HasFeasibleMaterialsForTarget(player, spell, ritualCard))
            return false;

        int effectiveReq = GetEffectiveMaterialLevelRequirement(spell, ritualCard);
        RitualMaterialLevelCompare effectiveCompare = GetEffectiveMaterialLevelCompare(spell);

        string matKey = effectiveCompare == RitualMaterialLevelCompare.AtLeast
            ? "RITUAL_SUMMON_MATERIALS_AT_LEAST"
            : "RITUAL_SUMMON_MATERIALS_EXACT";
        var matPrompt = new LocString("combat_messages", matKey);
        matPrompt.Add("Requirement", (decimal)effectiveReq);

        int minMatPick = spell.ExactMaterialCardCount ?? 1;
        int maxMatPick = spell.ExactMaterialCardCount ?? Math.Max(1, materials.Count);
        var matPrefs = new CardSelectorPrefs(matPrompt, minMatPick, maxMatPick)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> matPick;
        try
        {
            matPick = await CardSelectCmd.FromSimpleGrid(ctx, materials, player, matPrefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        List<BaseMonsterCard> picked = matPick.OfType<BaseMonsterCard>().ToList();
        if (picked.Count == 0)
            return false;

        if (spell.ExactMaterialCardCount is int emc && picked.Count != emc)
            return false;

        if (picked.Distinct().Count() != picked.Count)
            return false;

        if (picked.Any(p => ReferenceEquals(p, ritualCard)))
            return false;

        int sum = picked.Sum(p => p.GetEffectiveDuelMonsterLevel());
        if (!LevelSumValid(sum, effectiveReq, effectiveCompare))
            return false;

        RitualSpellPlayPayload.SetPending(spell, new RitualSpellPendingResolution(ritualCard, picked));
        return true;
    }

    /// <summary>Releases materials and ritual-summons; called from <see cref="RitualSpellCard"/> during <see cref="BaseSpellCard.OnPlay"/>.</summary>
    public static async Task ApplyResolvedRitualAsync(
        Player player,
        RitualSpellPendingResolution resolution,
        PlayerChoiceContext choiceContext)
    {
        foreach (BaseMonsterCard m in resolution.Materials)
        {
            Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, m);
            if (pet != null)
            {
                await CreatureCmd.Kill(pet, force: true);
                continue;
            }

            if (m.Pile?.Type == PileType.Hand)
            {
                CardPile? gy = GraveyardPile.CustomType.GetPile(player);
                if (gy == null)
                    return;

                await CardPileCmd.Add(
                    new CardModel[] { m },
                    gy,
                    CardPilePosition.Top,
                    m,
                    false);
            }
            else
            {
                return;
            }
        }

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, resolution.RitualTarget, choiceContext);
    }
}
