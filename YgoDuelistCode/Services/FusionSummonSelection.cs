using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

public static class FusionSummonSelection
{
    private static readonly AsyncLocal<bool> CompletingFusionSpellPlay = new();

    private static readonly LocString PickTargetPrompt = new LocString("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    private sealed class ReferenceCardComparer : IEqualityComparer<BaseMonsterCard>
    {
        internal static readonly ReferenceCardComparer Instance = new();

        public bool Equals(BaseMonsterCard? x, BaseMonsterCard? y) => ReferenceEquals(x, y);

        public int GetHashCode(BaseMonsterCard obj) => RuntimeHelpers.GetHashCode(obj);
    }

    public static bool IsCompletingFusionSpellPlay => CompletingFusionSpellPlay.Value;

    public static void BeginCompletingFusionSpellPlay() => CompletingFusionSpellPlay.Value = true;

    public static void EndCompletingFusionSpellPlay() => CompletingFusionSpellPlay.Value = false;

    public static bool HasFeasibleFusionPlay(Player player, FusionSpellCard spell)
    {
        foreach (FusionMonsterCard target in GetFusionTargetsInExtraDeck(player, spell))
        {
            if (HasFeasibleMaterialsForFusion(player, spell, target))
                return true;
        }

        return false;
    }

    /// <summary>Extra Deck fusion monsters the spell can target and that have at least one legal material set right now.</summary>
    public static List<FusionMonsterCard> GetFeasibleFusionTargetsInExtraDeck(Player player, FusionSpellCard spell)
    {
        var list = new List<FusionMonsterCard>();
        foreach (FusionMonsterCard target in GetFusionTargetsInExtraDeck(player, spell))
        {
            if (HasFeasibleMaterialsForFusion(player, spell, target))
                list.Add(target);
        }

        return list;
    }

    public static List<FusionMonsterCard> GetFusionTargetsInExtraDeck(Player player, FusionSpellCard spell)
    {
        var list = new List<FusionMonsterCard>();
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        if (extra == null)
            return list;

        Type filter = spell.FusionTargetMonsterType;
        foreach (CardModel c in extra.Cards)
        {
            if (ReferenceEquals(c, spell))
                continue;
            if (c is FusionMonsterCard fm && filter.IsInstanceOfType(fm))
                list.Add(fm);
        }

        return list;
    }

    public static List<BaseMonsterCard> BuildMaterialCandidates(Player player, CardModel spellCard, FusionMonsterCard fusionTarget)
    {
        var list = new List<BaseMonsterCard>();
        foreach (BaseMonsterCard field in TributeSummonSelection.BuildTributeCandidateCards(player))
        {
            if (!ReferenceEquals(field, fusionTarget))
                list.Add(field);
        }

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null)
        {
            foreach (CardModel c in hand.Cards)
            {
                if (ReferenceEquals(c, spellCard) || ReferenceEquals(c, fusionTarget))
                    continue;
                if (c is BaseMonsterCard bm)
                    list.Add(bm);
            }
        }

        return list;
    }

    /// <summary>
    /// Hand + field monsters that can appear in at least one fully legal material combination for this fusion target
    /// (correct types, enough field space after releases).
    /// </summary>
    public static List<BaseMonsterCard> BuildValidMaterialCandidatesForGrid(
        Player player,
        FusionSpellCard spell,
        FusionMonsterCard fusionTarget)
    {
        List<BaseMonsterCard> mats = BuildMaterialCandidates(player, spell, fusionTarget);
        var usable = new HashSet<BaseMonsterCard>(ReferenceCardComparer.Instance);
        foreach (List<BaseMonsterCard> subset in FeasibleFusionMaterialSubsets(player, spell, fusionTarget, mats))
        {
            foreach (BaseMonsterCard c in subset)
                usable.Add(c);
        }

        var ordered = new List<BaseMonsterCard>();
        foreach (BaseMonsterCard c in mats)
        {
            if (usable.Contains(c))
                ordered.Add(c);
        }

        return ordered;
    }

    private static IEnumerable<List<BaseMonsterCard>> FeasibleFusionMaterialSubsets(
        Player player,
        FusionSpellCard spell,
        FusionMonsterCard fusionTarget,
        List<BaseMonsterCard>? mats = null)
    {
        IReadOnlyList<Type> req = fusionTarget.FusionMaterialTypes;
        if (req.Count == 0)
            yield break;

        mats ??= BuildMaterialCandidates(player, spell, fusionTarget);
        foreach (List<BaseMonsterCard> subset in Combinations(mats, req.Count))
        {
            if (!MaterialsMatchMultiset(req, subset))
                continue;
            int fieldTributes = subset.Count(m => TributeSummonSelection.ResolvePetForFieldCard(player, m) != null);
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, fieldTributes))
                continue;
            yield return subset;
        }
    }

    private static bool HasFeasibleMaterialsForFusion(Player player, FusionSpellCard spell, FusionMonsterCard fusionTarget) =>
        FeasibleFusionMaterialSubsets(player, spell, fusionTarget).Any();

    /// <summary>Assign each required type to a distinct picked monster (multiset).</summary>
    public static bool MaterialsMatchMultiset(IReadOnlyList<Type> requiredTypes, List<BaseMonsterCard> picked)
    {
        if (requiredTypes.Count != picked.Count)
            return false;
        return TryMatch(requiredTypes, picked, 0, new bool[picked.Count]);
    }

    private static bool TryMatch(IReadOnlyList<Type> req, List<BaseMonsterCard> pick, int i, bool[] used)
    {
        if (i >= req.Count)
            return true;
        Type need = req[i];
        for (int j = 0; j < pick.Count; j++)
        {
            if (used[j])
                continue;
            if (!need.IsInstanceOfType(pick[j]))
                continue;
            used[j] = true;
            if (TryMatch(req, pick, i + 1, used))
                return true;
            used[j] = false;
        }

        return false;
    }

    private static IEnumerable<List<BaseMonsterCard>> Combinations(IReadOnlyList<BaseMonsterCard> pool, int k)
    {
        int n = pool.Count;
        if (k < 0 || k > n)
            yield break;
        var idx = new int[k];
        for (int i = 0; i < k; i++)
            idx[i] = i;

        while (true)
        {
            yield return idx.Select(i => pool[i]).ToList();
            int t = k - 1;
            while (t >= 0 && idx[t] == t + n - k)
                t--;
            if (t < 0)
                yield break;
            idx[t]++;
            for (int j = t + 1; j < k; j++)
                idx[j] = idx[j - 1] + 1;
        }
    }

    public static async Task<bool> TrySelectFusionResolutionAsync(Player player, FusionSpellCard spell)
    {
        var ctx = new BlockingPlayerChoiceContext();

        List<FusionMonsterCard> targets = GetFeasibleFusionTargetsInExtraDeck(player, spell);
        if (targets.Count == 0)
            return false;

        bool needTargetGrid = spell.RequiresPlayerFusionTargetSelection || targets.Count > 1;

        FusionMonsterCard fusionCard;
        if (needTargetGrid)
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

            FusionMonsterCard? picked = targetPick.OfType<FusionMonsterCard>().FirstOrDefault();
            if (picked == null || !spell.FusionTargetMonsterType.IsInstanceOfType(picked))
                return false;
            fusionCard = picked;
        }
        else
        {
            fusionCard = targets[0];
        }

        IReadOnlyList<Type> req = fusionCard.FusionMaterialTypes;
        if (req.Count == 0)
            return false;

        List<BaseMonsterCard> materials = BuildValidMaterialCandidatesForGrid(player, spell, fusionCard);
        if (materials.Count < req.Count)
            return false;

        var matPrompt = new LocString("combat_messages", "FUSION_SUMMON_PICK_MATERIALS");
        matPrompt.Add("Count", (decimal)req.Count);

        var matPrefs = new CardSelectorPrefs(matPrompt, req.Count, req.Count)
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

        List<BaseMonsterCard> pickedMats = matPick.OfType<BaseMonsterCard>().ToList();
        if (pickedMats.Count != req.Count)
            return false;
        if (pickedMats.Distinct().Count() != pickedMats.Count)
            return false;
        if (pickedMats.Any(p => ReferenceEquals(p, fusionCard)))
            return false;
        if (!MaterialsMatchMultiset(req, pickedMats))
            return false;

        FusionSpellPlayPayload.SetPending(spell, new FusionSpellPendingResolution(fusionCard, pickedMats));
        return true;
    }

    public static async Task ApplyResolvedFusionAsync(
        Player player,
        FusionSpellPendingResolution resolution,
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

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, resolution.FusionTarget, choiceContext);
    }
}
