using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
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

    public static bool HasFeasibleFusionPlay(Player player, IFusionSpellSource spell)
    {
        var spellCard = (CardModel)(object)spell;
        foreach (FusionMonsterCard target in GetFusionTargetsInExtraDeck(player, spell, spellCard))
        {
            if (HasFeasibleMaterialsForFusion(player, spell, spellCard, target))
                return true;
        }

        return false;
    }

    /// <summary>Extra Deck fusion monsters the spell can target and that have at least one legal material set right now.</summary>
    public static List<FusionMonsterCard> GetFeasibleFusionTargetsInExtraDeck(Player player, IFusionSpellSource spell)
    {
        var spellCard = (CardModel)(object)spell;
        var list = new List<FusionMonsterCard>();
        foreach (FusionMonsterCard target in GetFusionTargetsInExtraDeck(player, spell, spellCard))
        {
            if (HasFeasibleMaterialsForFusion(player, spell, spellCard, target))
                list.Add(target);
        }

        return list;
    }

    public static List<FusionMonsterCard> GetFusionTargetsInExtraDeck(Player player, IFusionSpellSource spell, CardModel spellCard)
    {
        var list = new List<FusionMonsterCard>();
        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        if (extra == null)
            return list;

        Type filter = spell.FusionTargetMonsterType;
        // MP: pile iteration order is not guaranteed to match across peers; stabilize before feasibility checks.
        YgoNetCombatCardPileGate.EnsureMutableCombatCardsHaveNetIds(extra.Cards);
        foreach (CardModel c in extra.Cards.OrderBy(x => NetCombatCardDb.Instance.GetCardId(x)))
        {
            if (ReferenceEquals(c, spellCard))
                continue;
            if (c is FusionMonsterCard fm && fm.CanBeFusionSummoned && filter.IsInstanceOfType(fm))
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
        IFusionSpellSource spell,
        FusionMonsterCard fusionTarget)
    {
        var spellCard = (CardModel)(object)spell;
        List<BaseMonsterCard> mats = BuildMaterialCandidates(player, spellCard, fusionTarget);
        var usable = new HashSet<BaseMonsterCard>(ReferenceCardComparer.Instance);
        foreach (List<BaseMonsterCard> subset in FeasibleFusionMaterialSubsets(player, spell, spellCard, fusionTarget, mats))
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
        IFusionSpellSource spell,
        CardModel spellCard,
        FusionMonsterCard fusionTarget,
        List<BaseMonsterCard>? mats = null)
    {
        IReadOnlyList<FusionMaterialSlot> slots = fusionTarget.FusionMaterialSlots;
        if (slots.Count == 0)
            yield break;

        mats ??= BuildMaterialCandidates(player, spellCard, fusionTarget);
        foreach (List<BaseMonsterCard> subset in Combinations(mats, slots.Count))
        {
            if (!FusionMaterialSlotMatching.MaterialsMatchSlots(slots, subset))
                continue;
            int fieldTributes = subset.Count(m => TributeSummonSelection.ResolvePetForFieldCard(player, m) != null);
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, fieldTributes))
                continue;
            yield return subset;
        }
    }

    private static bool HasFeasibleMaterialsForFusion(
        Player player,
        IFusionSpellSource spell,
        CardModel spellCard,
        FusionMonsterCard fusionTarget) =>
        FeasibleFusionMaterialSubsets(player, spell, spellCard, fusionTarget).Any();

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

    public static async Task<bool> TrySelectFusionResolutionAsync(Player player, IFusionSpellSource spell)
    {
        var spellCard = (CardModel)(object)spell;
        var ctx = new BlockingPlayerChoiceContext();

        List<FusionMonsterCard> targets = GetFeasibleFusionTargetsInExtraDeck(player, spell);
        if (targets.Count == 0)
            return false;

        bool needTargetGrid = spell.RequiresPlayerFusionTargetSelection || targets.Count > 1;
        NetGameType net = RunManager.Instance?.NetService.Type ?? NetGameType.None;
        bool mp = net is NetGameType.Host or NetGameType.Client;
        if (mp)
        {
            GD.Print(
                $"[YgoDuelist][MP][Fusion] TrySelect start spell={spellCard.Id?.Entry} ownerNet={player.NetId} feasibleFusionTargets={targets.Count} needTargetGrid={needTargetGrid} net={net}");
        }

        List<CardModel> feasibleStable =
            TributeSummonGridSelect.StabilizeHandPileCandidates(targets.Cast<CardModel>());

        FusionMonsterCard fusionCard;
        if (needTargetGrid)
        {
            var targetPrefs = new CardSelectorPrefs(PickTargetPrompt, 1, 1) { Cancelable = true };
            IEnumerable<CardModel> targetPick;
            try
            {
                targetPick = await TributeSummonGridSelect.FromSimpleGridCombat(
                    ctx,
                    feasibleStable,
                    player,
                    targetPrefs,
                    rebuildCanonicalForRemoteApply: () =>
                        TributeSummonGridSelect.StabilizeHandPileCandidates(
                            GetFeasibleFusionTargetsInExtraDeck(player, spell).Cast<CardModel>()));
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
        else if (mp && feasibleStable.Count == 1)
        {
            // MP lockstep: always take the fusion-target step through PlayerChoiceSynchronizer + combat wire, even when
            // only one legal fusion exists. Otherwise peers that skip this ReserveChoiceId (while the caster shows a
            // multi-target grid) assign different choice ids to the material grid and apply wrong buffered results.
            var targetPrefs = new CardSelectorPrefs(PickTargetPrompt, 1, 1) { Cancelable = true };
            IEnumerable<CardModel> targetPick;
            try
            {
                targetPick = await TributeSummonGridSelect.FromSimpleGridCombat(
                    ctx,
                    feasibleStable,
                    player,
                    targetPrefs,
                    rebuildCanonicalForRemoteApply: () =>
                        TributeSummonGridSelect.StabilizeHandPileCandidates(
                            GetFeasibleFusionTargetsInExtraDeck(player, spell).Cast<CardModel>()));
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
            if (feasibleStable[0] is not FusionMonsterCard single)
                return false;
            fusionCard = single;
        }

        IReadOnlyList<FusionMaterialSlot> slots = fusionCard.FusionMaterialSlots;
        if (slots.Count == 0)
            return false;

        List<BaseMonsterCard> materials = BuildValidMaterialCandidatesForGrid(player, spell, fusionCard);
        if (materials.Count < slots.Count)
            return false;

        var matPrompt = new LocString("combat_messages", "FUSION_SUMMON_PICK_MATERIALS");
        matPrompt.Add("Count", (decimal)slots.Count);

        var matPrefs = new CardSelectorPrefs(matPrompt, slots.Count, slots.Count)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<CardModel> materialsStable =
            TributeSummonGridSelect.StabilizeHandPileCandidates(materials.Cast<CardModel>());

        IEnumerable<CardModel> matPick;
        try
        {
            matPick = await TributeSummonGridSelect.FromSimpleGridCombat(
                ctx,
                materialsStable,
                player,
                matPrefs,
                rebuildCanonicalForRemoteApply: () =>
                    TributeSummonGridSelect.StabilizeHandPileCandidates(
                        BuildValidMaterialCandidatesForGrid(player, spell, fusionCard).Cast<CardModel>()));
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        List<BaseMonsterCard> pickedMats = matPick.OfType<BaseMonsterCard>().ToList();
        if (pickedMats.Count != slots.Count)
            return false;
        if (pickedMats.Distinct().Count() != pickedMats.Count)
            return false;
        if (pickedMats.Any(p => ReferenceEquals(p, fusionCard)))
            return false;
        if (!FusionMaterialSlotMatching.MaterialsMatchSlots(slots, pickedMats))
            return false;

        if (!YgoPlayPayloadNetKey.TryGetKey(spellCard, out ulong ownerNetId, out uint combatIdx))
        {
            Godot.GD.PrintErr($"[YgoDuelist][MP] FusionSummonSelection: could not get NetCombatCard key for spell {spellCard.Id?.Entry}; fusion payload not stored");
            return false;
        }

        FusionSpellPlayPayload.SetPending(ownerNetId, combatIdx, new FusionSpellPendingResolution(fusionCard, pickedMats));
        Godot.GD.Print(
            $"[YgoDuelist][MP][Fusion] SetPending spell={spellCard.Id?.Entry} key=({ownerNetId},{combatIdx}) fusion={fusionCard.Id?.Entry} mats={pickedMats.Count}");
        return true;
    }

    public static async Task ApplyResolvedFusionAsync(
        Player player,
        IFusionSpellSource spell,
        FusionSpellPendingResolution resolution,
        PlayerChoiceContext choiceContext)
    {
        bool banish = spell.BanishesFusionMaterials;
        foreach (BaseMonsterCard m in resolution.Materials)
        {
            Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, m);
            if (pet != null)
            {
                await CreatureCmd.Kill(pet, force: true);
                if (banish)
                    await YgoBanishedService.BanishCard(player, m);
                continue;
            }

            if (m.Pile?.Type == PileType.Hand)
            {
                if (banish)
                {
                    await YgoBanishedService.BanishCard(player, m);
                }
                else
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
            }
            else
            {
                return;
            }
        }

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, resolution.FusionTarget, choiceContext);
    }
}
