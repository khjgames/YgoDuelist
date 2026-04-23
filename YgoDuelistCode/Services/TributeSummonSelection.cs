using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class TributeSummonSelection
{
    private static readonly LocString TributePrompt = new LocString("combat_messages", "TRIBUTE_SUMMON_SELECT");

    /// <summary>
    /// Hand normal monster with tribute cost uses a cancelable material grid before spend/OnPlay.
    /// Queue timing: see <see cref="YgoPlayCardQueueDeferral"/>.
    /// </summary>
    public static bool IsHandTributeDuelNormalSummonPlay(PlayCardAction action)
    {
        if (!CombatManager.Instance.IsInProgress)
            return false;

        CardModel? card = action.NetCombatCard.ToCardModelOrNull();
        if (card == null)
            return false;
        return card is NormalMonsterCard nmc && nmc.CanSummonDuelMonster && nmc.TributeReleaseCount > 0
            && card.Pile?.Type == PileType.Hand;
    }

    private const int MaxInvalidTributeReselects = 16;

    /// <summary>Whether the field can pay <see cref="BaseMonsterCard.TributeReleaseCount"/> for this summon (including double-tribute materials).</summary>
    public static bool CanMeetTributeCostForSummon(Player? player, BaseMonsterCard summon)
    {
        if (summon is IYgoNamedTripleTributeSummon named)
            return named.CanMeetNamedTripleTributeRequirement(player);

        int need = summon.TributeReleaseCount;
        if (need <= 0)
            return true;
        if (player?.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> field = BuildTributeCandidateCards(player);
        bool mausoleumActive = YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Mausoleum_of_the_Emperor>(player);
        int hpOptions = mausoleumActive ? 3 : 0;
        int hpPerTribute = mausoleumActive ? GetMausoleumOptionHpLoss(player) : 0;
        int currentHp = player.Creature?.CurrentHp ?? 0;
        int minPets = MinFieldPetsNeededToPayTributeWithMausoleum(field, summon, hpOptions);
        if (minPets == int.MaxValue)
            return false;
        int minHpTributes = need - GetMaxFieldTributeContributionForPetCount(field, summon, minPets);
        if (minHpTributes < 0)
            minHpTributes = 0;
        if (minHpTributes > hpOptions)
            return false;
        if (hpPerTribute > 0 && currentHp < (minHpTributes * hpPerTribute))
            return false;

        return DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, minPets);
    }

    /// <summary>Tribute selection for a normal summon, allowing one double-tribute monster when the summon needs two releases.</summary>
    public static async Task<TributeSummonPendingResolution?> SelectTributesForNormalSummonAsync(Player player, BaseMonsterCard summonCard)
    {
        int need = summonCard.TributeReleaseCount;
        if (need <= 0)
            return new TributeSummonPendingResolution(new List<Creature>(), 0, 0);

        for (int attempt = 0; attempt < MaxInvalidTributeReselects; attempt++)
        {
            TributeSummonPendingResolution? resolution = await TrySelectVariableTributeAsync(player, need, summonCard);
            if (resolution == null)
                return null;
            if (TributeSelectionMeetsCost(summonCard, player, resolution.Pets, resolution.MausoleumHpTributes, resolution.MausoleumHpLossTotal))
                return resolution;
        }

        return null;
    }

    public static bool TributeSelectionMeetsCost(
        BaseMonsterCard summon,
        Player? player,
        List<Creature>? pets,
        int mausoleumHpTributes,
        int mausoleumHpLossTotal)
    {
        if (summon is IYgoNamedTripleTributeSummon namedSummon)
            return namedSummon.NamedTributeRecipeMatches(pets, mausoleumHpTributes, mausoleumHpLossTotal);

        int need = summon.TributeReleaseCount;
        if (need <= 0)
            return true;
        if (mausoleumHpTributes < 0 || mausoleumHpTributes > 3)
            return false;
        if (mausoleumHpLossTotal < 0)
            return false;
        if (pets == null)
            return false;
        if (pets.Count + mausoleumHpTributes > need)
            return false;
        int currentHp = player?.Creature?.CurrentHp ?? 0;
        if (currentHp < mausoleumHpLossTotal)
            return false;

        var field = new List<BaseMonsterCard>(pets.Count);
        foreach (Creature pet in pets)
        {
            BaseMonsterCard? c = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
            if (c == null)
                return false;
            field.Add(c);
        }

        if (field.Distinct().Count() != field.Count)
            return false;

        int fieldContribution = field.Sum(m => DoubleTributeTributeMath.GetTributeContribution(m, summon));
        return fieldContribution + mausoleumHpTributes == need;
    }

    private static async Task<TributeSummonPendingResolution?> TrySelectVariableTributeAsync(Player player, int need, BaseMonsterCard summonCard)
    {
        List<CardModel> candidates = BuildTributeSelectionCandidates(player, summonCard, need);
        if (candidates.Count == 0)
            return null;

        int minPick = summonCard.MinTributeSelectionPickCount(need);
        var prefs = new CardSelectorPrefs(TributePrompt, minCount: minPick, maxCount: need)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        Func<List<CardModel>> rebuildCanonical =
            () => BuildTributeSelectionCandidates(player, summonCard, need).Cast<CardModel>().ToList();

        // Field-only tribute grids: same combat-card wire as fusion/ritual (GridCombatMpExpectation, FromMutableCombatCards).
        // Mausoleum synthetic rows are not stable on the combat-card net path — keep Index wire + rebuild for those.
        bool useCombatWire = candidates.TrueForAll(c => c is not IYgoMausoleumHpTributeOption);
        if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            GD.Print(
                $"[YgoDuelist][MP][Tribute] TrySelectVariableTribute owner={player.NetId} summon={summonCard.Id?.Entry} need={need} combatWire={useCombatWire} candidates={candidates.Count}");
        }

        IEnumerable<CardModel> selected;
        try
        {
            selected = useCombatWire
                ? await TributeSummonGridSelect.FromSimpleGridCombat(
                    YgoChoiceContexts.Blocking(),
                    candidates,
                    player,
                    prefs,
                    rebuildCanonicalForRemoteApply: rebuildCanonical)
                : await TributeSummonGridSelect.FromSimpleGridIndexed(
                    YgoChoiceContexts.Blocking(),
                    candidates,
                    player,
                    prefs,
                    rebuildCanonicalForRemoteApply: rebuildCanonical);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        var picked = selected.ToList();
        if (picked.Count == 0 || picked.Count > need)
            return null;

        List<BaseMonsterCard> pickedField = picked.OfType<BaseMonsterCard>().ToList();
        int mausoleumHpTributes = picked.OfType<IYgoMausoleumHpTributeOption>().Count();
        if (pickedField.Count + mausoleumHpTributes != picked.Count)
            return null;
        if (pickedField.Distinct().Count() != pickedField.Count)
            return null;
        if (mausoleumHpTributes > 3)
            return null;

        var pets = new List<Creature>(pickedField.Count);
        foreach (BaseMonsterCard c in pickedField)
        {
            Creature? pet = ResolvePetForFieldCard(player, c);
            if (pet == null || !pet.IsAlive)
                return null;
            pets.Add(pet);
        }

        if (pets.Distinct().Count() != pets.Count)
            return null;

        int mausoleumHpLossTotal = picked.OfType<IYgoMausoleumHpTributeOption>().Sum(c => c.TributeHpLoss);
        if (!TributeSelectionMeetsCost(summonCard, player, pets, mausoleumHpTributes, mausoleumHpLossTotal))
            return new TributeSummonPendingResolution(new List<Creature>(), 0, 0);

        return new TributeSummonPendingResolution(pets, mausoleumHpTributes, mausoleumHpLossTotal);
    }

    private static List<CardModel> BuildTributeSelectionCandidates(Player player, BaseMonsterCard summonCard, int need)
    {
        List<CardModel> candidates = YgoMpCombatOrder.CardsSnapshotOrderedForMp(BuildTributeCandidateCards(player));
        if (summonCard.AllowsMausoleumHpTributeForThisTributeSummon
            && YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Mausoleum_of_the_Emperor>(player)
            && player.Creature?.CombatState != null)
        {
            bool upgradedOptions = HasUpgradedActiveMausoleum(player);
            for (int i = 0; i < 3 && i < need; i++)
            {
                var option = player.Creature.CombatState.CreateCard<Mausoleum_Lose_HP>(player);
                option.MausoleumGridSlot = i;
                if (summonCard is NormalMonsterCard normalSource)
                    option.InitializeSource(normalSource);
                if (upgradedOptions)
                    option.UpgradeInternal();
                candidates.Add(option);
            }
        }

        StabilizeFullTributeCandidateList(candidates);
        summonCard.FilterTributeSelectionGridCandidates(player, candidates, need);
        return candidates;
    }

    /// <summary>
    /// Field monsters are already sorted; Mausoleum rows must have a fixed order on every peer for
    /// <see cref="TributeSummonGridSelect.FromSimpleGridIndexed"/> MP sync.
    /// </summary>
    private static void StabilizeFullTributeCandidateList(List<CardModel> candidates)
    {
        var field = new List<CardModel>();
        var hp = new List<CardModel>();
        foreach (CardModel c in candidates)
        {
            if (c is IYgoMausoleumHpTributeOption)
                hp.Add(c);
            else
                field.Add(c);
        }

        hp.Sort((a, b) =>
            ((IYgoMausoleumHpTributeOption)a).MausoleumGridSlot.CompareTo(((IYgoMausoleumHpTributeOption)b).MausoleumGridSlot));
        candidates.Clear();
        candidates.AddRange(field);
        candidates.AddRange(hp);
    }

    private static int MinFieldPetsNeededToPayTributeWithMausoleum(
        IReadOnlyList<BaseMonsterCard> field,
        BaseMonsterCard summon,
        int mausoleumHpOptions)
    {
        int need = summon.TributeReleaseCount;
        if (need <= 0)
            return 0;
        if (mausoleumHpOptions >= need)
            return 0;

        int best = int.MaxValue;
        foreach (List<BaseMonsterCard> subset in EnumerateDistinctSubsets(field, maxCount: need))
        {
            int fieldCount = subset.Count;
            int fieldContribution = subset.Sum(m => DoubleTributeTributeMath.GetTributeContribution(m, summon));
            int hpNeeded = need - fieldContribution;
            if (hpNeeded < 0)
                continue;
            if (hpNeeded > mausoleumHpOptions)
                continue;
            if (fieldCount < best)
                best = fieldCount;
        }

        return best;
    }

    private static int GetMaxFieldTributeContributionForPetCount(
        IReadOnlyList<BaseMonsterCard> field,
        BaseMonsterCard summon,
        int fieldPetCount)
    {
        int best = 0;
        foreach (List<BaseMonsterCard> subset in EnumerateDistinctSubsets(field, maxCount: fieldPetCount))
        {
            if (subset.Count != fieldPetCount)
                continue;
            int contribution = subset.Sum(m => DoubleTributeTributeMath.GetTributeContribution(m, summon));
            if (contribution > best)
                best = contribution;
        }

        return best;
    }

    private static bool HasUpgradedActiveMausoleum(Player? player) =>
        YgoFieldSpellStatAggregator
            .GetActiveFaceUpFieldSpells(player)
            .OfType<Mausoleum_of_the_Emperor>()
            .Any(m => m.IsUpgraded);

    private static int GetMausoleumOptionHpLoss(Player? player) =>
        HasUpgradedActiveMausoleum(player)
            ? Mausoleum_Lose_HP.UpgradedTributeHpLoss
            : Mausoleum_Lose_HP.BaseTributeHpLoss;

    private static IEnumerable<List<BaseMonsterCard>> EnumerateDistinctSubsets(IReadOnlyList<BaseMonsterCard> pool, int maxCount)
    {
        yield return new List<BaseMonsterCard>();
        int n = pool.Count;
        int upper = maxCount < n ? maxCount : n;
        for (int k = 1; k <= upper; k++)
        {
            int[] idx = new int[k];
            for (int i = 0; i < k; i++)
                idx[i] = i;
            while (true)
            {
                var subset = new List<BaseMonsterCard>(k);
                for (int i = 0; i < k; i++)
                    subset.Add(pool[idx[i]]);
                yield return subset;

                int t = k - 1;
                while (t >= 0 && idx[t] == t + n - k)
                    t--;
                if (t < 0)
                    break;
                idx[t]++;
                for (int j = t + 1; j < k; j++)
                    idx[j] = idx[j - 1] + 1;
            }
        }
    }

    /// <summary>Living duel monsters on the field with a registered source card (tribute candidates).</summary>
    public static int CountTributableFieldMonsters(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return 0;

        int n = 0;
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) != null)
                n++;
        }

        return n;
    }

    /// <summary>
    /// Source cards for each tributable field monster. Sorted by <see cref="NetCombatCardDb.GetCardId"/> (then card id entry)
    /// so the tribute grid order matches on host and observers. <see cref="Creature.CombatId"/> order can differ across peers
    /// for the same pets; wire indexes must not depend on it.
    /// </summary>
    public static List<BaseMonsterCard> BuildTributeCandidateCards(Player player)
    {
        var list = new List<BaseMonsterCard>();
        if (player.PlayerCombatState == null)
            return list;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            BaseMonsterCard? c = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
            if (c != null)
                list.Add(c);
        }

        YgoNetCombatCardPileGate.EnsureMutableCombatCardsHaveNetIds(list);

        list.Sort((a, b) =>
        {
            uint idA = NetCombatCardDb.Instance.GetCardId(a);
            uint idB = NetCombatCardDb.Instance.GetCardId(b);
            int cmp = idA.CompareTo(idB);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Id?.Entry ?? "", b.Id?.Entry ?? "");
        });

        if (TributeSummonGridSelect.VerboseHandGridMpLog && RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            string order = string.Join(",", list.Select(c => $"{NetCombatCardDb.Instance.GetCardId(c)}:{c.Id?.Entry}"));
            GD.Print($"[YgoDuelist][MP][TributeCandidates] owner={player.NetId} sorted=[{order}]");
        }

        return list;
    }

    public static Creature? ResolvePetForFieldCard(Player player, BaseMonsterCard fieldSourceCard)
    {
        if (player.PlayerCombatState == null)
            return null;

        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (p.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.HasSourceCard(p, fieldSourceCard))
                return p;
        }

        return null;
    }

    /// <summary>
    /// Opens the simple grid to pick exactly <paramref name="tributeCount"/> field monsters.
    /// Returns null if the player cancelled or the task was canceled.
    /// </summary>
    public static async Task<List<Creature>?> SelectTributesAsync(Player player, int tributeCount)
    {
        if (tributeCount <= 0)
            return new List<Creature>();

        var candidates = BuildTributeCandidateCards(player);
        if (candidates.Count < tributeCount)
            return null;

        var prefs = new CardSelectorPrefs(TributePrompt, tributeCount, tributeCount)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> selected;
        try
        {
            selected = await TributeSummonGridSelect.FromSimpleGridIndexed(
                YgoChoiceContexts.Blocking(),
                candidates,
                player,
                prefs,
                rebuildCanonicalForRemoteApply: () =>
                    BuildTributeCandidateCards(player).Cast<CardModel>().ToList());
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        var picked = selected.OfType<BaseMonsterCard>().ToList();
        if (picked.Count != tributeCount)
            return null;

        if (picked.Distinct().Count() != tributeCount)
            return null;

        var pets = new List<Creature>(tributeCount);
        foreach (BaseMonsterCard c in picked)
        {
            Creature? pet = ResolvePetForFieldCard(player, c);
            if (pet == null || !pet.IsAlive)
                return null;
            pets.Add(pet);
        }

        if (pets.Distinct().Count() != tributeCount)
            return null;

        return pets;
    }
}
