using System.Reflection;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YgoDuelist card rewards: combat encounters use pack size by tier; Neow Draft (and the same CardCreationOptions pattern) use
/// <see cref="NeowBlessingPackSlots"/> per pack. Three packs, then grids for deck → side deck → trunk.
/// Returns <c>true</c> from <see cref="CardReward.OnSelect"/> when finished so the reward is consumed (vanilla behavior).
/// </summary>
public static class YgoCardPackRewardFlow
{
    public const int NeowBlessingPackSlots = 3;

    /// <summary>
    /// When <c>true</c>, pack choice uses <see cref="CardSelectCmd.FromChooseABundleScreen"/> (full visible bundles).
    /// When <c>false</c>, pack choice uses <see cref="ChoosePackWithoutVisibleBundlesAsync"/> instead.
    /// </summary>
    public const bool Pack_Style_Visible_Bundles = false;

    private static readonly BindingFlags RewardMemberFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static bool ShouldReplaceCardRewardSelection(CardReward reward)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(reward.Player))
            return false;
        CardCreationOptions options = GetCardCreationOptions(reward);
        if (options.Source == CardCreationSource.Encounter)
            return true;
        return IsNeowBlessingStyleCardReward(options, reward);
    }

    /// <summary>
    /// Matches vanilla <c>Draft</c> Neow blessing: <see cref="CardCreationSource.Other"/>, <see cref="CardRarityOddsType.RegularEncounter"/>,
    /// <see cref="CardCreationFlags.NoUpgradeRoll"/>, character card pools only, three options. Excludes Orrery/Lost Coffer (no NoUpgradeRoll).
    /// </summary>
    private static bool IsNeowBlessingStyleCardReward(CardCreationOptions options, CardReward reward)
    {
        if (options.Source != CardCreationSource.Other)
            return false;
        if (options.RarityOdds != CardRarityOddsType.RegularEncounter)
            return false;
        if (!options.Flags.HasFlag(CardCreationFlags.NoUpgradeRoll))
            return false;
        if (options.Flags.HasFlag(CardCreationFlags.ForceRarityOddsChange))
            return false;
        if (options.CustomCardPool != null)
            return false;
        return GetOptionCount(reward) == NeowBlessingPackSlots;
    }

    public static async Task<bool> RunAsync(CardReward reward)
    {
        Player player = reward.Player;

        if (CombatManager.Instance!.IsEnding)
        {
            UnsubscribeRelicHandler(reward, player);
            return false;
        }

        CardCreationOptions options = GetCardCreationOptions(reward);
        int slotCount = GetPackSlotCount(options, reward);
        Rng rng = player.PlayerRng.Rewards;
        var choiceContext = new BlockingPlayerChoiceContext();

        LogPackFlowPhase(
            player,
            "flow_start",
            $"cardSource={options.Source} | rarityOdds={options.RarityOdds} | slotCount={slotCount} | canSkipReward={reward.CanSkip}");

        var bundles = new List<IReadOnlyList<CardModel>>(3);

        void BuildBundlesFromGenerator()
        {
            bundles.Clear();
            List<List<CardModel>> templatePacks = YgoCardPackGenerator.GenerateThreePackTemplates(
                player,
                rng,
                slotCount,
                options.RarityOdds);
            foreach (List<CardModel> pack in templatePacks)
            {
                var row = new List<CardModel>(pack.Count);
                foreach (CardModel template in pack)
                    row.Add(player.RunState.CreateCard(template, player));
                bundles.Add(row);
            }
        }

        BuildBundlesFromGenerator();

        List<CardModel> chosenPack;
        int chosenBundleIndex;

    PickBundle:
        LogPackFlowPhase(player, "choose_pack_phase_start", SummarizeBundlesForLog(bundles));
        try
        {
            chosenPack = Pack_Style_Visible_Bundles
                ? (await CardSelectCmd.FromChooseABundleScreen(player, bundles)).ToList()
                : await ChoosePackWithoutVisibleBundlesAsync(player, choiceContext, bundles, reward.CanSkip);
        }
        catch (OperationCanceledException)
        {
            RemoveAllCreatedCards(bundles, player);
            LogPackFlowPhase(player, "flow_end_cancelled_choose_pack", "removed preview clones");
            return false;
        }

        if (chosenPack.Count == 0)
        {
            RemoveAllCreatedCards(bundles, player);
            UnsubscribeRelicHandler(reward, player);
            RecordAllPackCardsSkippedForReward(player, bundles);
            LogPackFlowPhase(player, "flow_end_skip_no_pack", "removed preview clones; reward consumed");
            return true;
        }

        chosenBundleIndex = IndexOfBundleByInstanceSequence(bundles, chosenPack);
        LogPackFlowPhase(
            player,
            "choose_pack_phase_end",
            $"chosenBundleIndex={chosenBundleIndex} | chosenSize={chosenPack.Count} | {SummarizeRarities(chosenPack)}");

        var deckPrefs = new CardSelectorPrefs(
            new LocString("combat_messages", "YGODUELIST-PACK_REWARD_DECK.prompt"),
            0,
            chosenPack.Count)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<CardModel> deckPicks;

    AssignDeck:
        LogPackFlowPhase(
            player,
            "assign_deck_phase_start",
            $"poolSize={chosenPack.Count} | {SummarizeRarities(chosenPack)}");
        try
        {
            deckPicks = (await CardSelectCmd.FromSimpleGrid(choiceContext, chosenPack, player, deckPrefs)).ToList();
        }
        catch (OperationCanceledException)
        {
            LogPackFlowPhase(player, "assign_deck_cancelled_back_to_choose_pack", "");
            goto PickBundle;
        }

        LogPackFlowPhase(
            player,
            "assign_deck_phase_end",
            $"deckPicks={deckPicks.Count} | {SummarizeRarities(deckPicks)}");

        var deckSet = new HashSet<CardModel>(deckPicks);
        List<CardModel> remainder = chosenPack.Where(c => !deckSet.Contains(c)).ToList();
        List<CardModel> sidePicks;
        if (remainder.Count == 0)
        {
            sidePicks = [];
            LogPackFlowPhase(player, "assign_side_phase_skipped", "no remainder after deck");
            goto ApplyPackReward;
        }

        var sidePrefs = new CardSelectorPrefs(
            new LocString("combat_messages", "YGODUELIST-PACK_REWARD_SIDE.prompt"),
            0,
            remainder.Count)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        LogPackFlowPhase(
            player,
            "assign_side_phase_start",
            $"remainderSize={remainder.Count} | {SummarizeRarities(remainder)}");
        try
        {
            sidePicks = (await CardSelectCmd.FromSimpleGrid(choiceContext, remainder, player, sidePrefs)).ToList();
        }
        catch (OperationCanceledException)
        {
            LogPackFlowPhase(player, "assign_side_cancelled_back_to_assign_deck", "");
            goto AssignDeck;
        }

        LogPackFlowPhase(
            player,
            "assign_side_phase_end",
            $"sidePicks={sidePicks.Count} | {SummarizeRarities(sidePicks)}");

    ApplyPackReward:
        LogPackFlowPhase(
            player,
            "apply_reward_phase_start",
            $"deck={deckPicks.Count} side={sidePicks.Count} trunkFromPack={chosenPack.Count - deckPicks.Count - sidePicks.Count}");
        UnsubscribeRelicHandler(reward, player);

        YgoPlayerMinimumDeck.IncreaseAfterPackRewardConfirmed(player);

        var history = player.RunState.CurrentMapPointHistoryEntry!.GetEntry(LocalContext.NetId!.Value);
        var deckSetFinal = new HashSet<CardModel>(deckPicks);

        foreach (CardModel card in deckPicks)
        {
            CardPileAddResult add = await CardPileCmd.Add(card, PileType.Deck);
            if (!add.success)
                continue;
            CardModel added = add.cardAdded;
            Log.Info($"[YgoDuelist] Pack reward deck {added.Id}");
            RunManager.Instance!.RewardSynchronizer.SyncLocalObtainedCard(added);
            history.CardChoices.Add(new CardChoiceHistoryEntry(added, wasPicked: true));
        }

        CardPile sidePile = PlayerRunSideDeck.GetOrCreatePile(player);
        foreach (CardModel c in sidePicks)
        {
            c.FloorAddedToDeck = player.RunState.TotalFloor;
            sidePile.AddInternal(c, -1, silent: true);
            Log.Info($"[YgoDuelist] Pack reward side deck {c.Id}");
            RunManager.Instance!.RewardSynchronizer.SyncLocalObtainedCard(c);
            history.CardChoices.Add(new CardChoiceHistoryEntry(c, wasPicked: true));
        }

        var sideSet = new HashSet<CardModel>(sidePicks);
        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(player);
        foreach (CardModel c in chosenPack)
        {
            if (deckSetFinal.Contains(c) || sideSet.Contains(c))
                continue;
            c.FloorAddedToDeck = player.RunState.TotalFloor;
            trunk.AddInternal(c, -1, silent: true);
            history.CardChoices.Add(new CardChoiceHistoryEntry(c, wasPicked: false));
            RunManager.Instance!.RewardSynchronizer.SyncLocalSkippedCard(c);
        }

        if (chosenBundleIndex >= 0)
        {
            for (int i = 0; i < bundles.Count; i++)
            {
                if (i == chosenBundleIndex)
                    continue;
                foreach (CardModel c in bundles[i])
                {
                    history.CardChoices.Add(new CardChoiceHistoryEntry(c, wasPicked: false));
                    RunManager.Instance!.RewardSynchronizer.SyncLocalSkippedCard(c);
                }
            }
        }

        player.Deck.InvokeCardAddFinished();
        TrunkSideDeckRelic.NotifyRunTrunkSideChanged(player);
        LogPackFlowPhase(player, "flow_end_success", "minDeck bump applied; cards committed");
        return true;
    }

    private static void LogPackFlowPhase(Player player, string phase, string detail)
    {
        int owed = YgoPackRewardProgress.For(player).OwedRareCardVouchers;
        string tail = string.IsNullOrEmpty(detail) ? "" : " | " + detail;
        Log.Info($"[YgoDuelist][PackFlow] phase={phase} | owedRareVouchers={owed}{tail}");
    }

    private static string SummarizeRarities(IReadOnlyList<CardModel> cards)
    {
        int c = cards.Count(x => x.Rarity == CardRarity.Common);
        int u = cards.Count(x => x.Rarity == CardRarity.Uncommon);
        int r = cards.Count(x => x.Rarity == CardRarity.Rare);
        return $"C={c} U={u} R={r}";
    }

    private static string SummarizeBundlesForLog(List<IReadOnlyList<CardModel>> bundles)
    {
        var parts = new List<string>(bundles.Count);
        for (int i = 0; i < bundles.Count; i++)
        {
            IReadOnlyList<CardModel> b = bundles[i];
            parts.Add($"[{i}] size={b.Count} {SummarizeRarities(b)}");
        }

        return string.Join("; ", parts);
    }

    private static void RemoveAllCreatedCards(List<IReadOnlyList<CardModel>> bundles, Player player)
    {
        foreach (IReadOnlyList<CardModel> b in bundles)
        {
            foreach (CardModel c in b)
                player.RunState.RemoveCard(c);
        }
    }

    private static void RecordAllPackCardsSkippedForReward(Player player, List<IReadOnlyList<CardModel>> bundles)
    {
        if (RunManager.Instance == null)
            return;
        var historyEntry = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(LocalContext.NetId!.Value);
        if (historyEntry == null)
            return;

        foreach (IReadOnlyList<CardModel> b in bundles)
        {
            foreach (CardModel c in b)
            {
                historyEntry.CardChoices.Add(new CardChoiceHistoryEntry(c, wasPicked: false));
                RunManager.Instance.RewardSynchronizer.SyncLocalSkippedCard(c);
            }
        }
    }

    /// <summary>
    /// Pack pick without <see cref="CardSelectCmd.FromChooseABundleScreen"/>: one representative card per pack on
    /// <see cref="CardSelectCmd.FromChooseACardScreen"/>, then the full bundle for that index. Same contract as the bundle screen
    /// (empty list = skip reward, <see cref="OperationCanceledException"/> on cancel).
    /// </summary>
    private static async Task<List<CardModel>> ChoosePackWithoutVisibleBundlesAsync(
        Player player,
        BlockingPlayerChoiceContext choiceContext,
        List<IReadOnlyList<CardModel>> bundles,
        bool canSkip)
    {
        if (CombatManager.Instance!.IsEnding)
            return [];

        var representatives = new List<CardModel>(bundles.Count);
        foreach (IReadOnlyList<CardModel> pack in bundles)
            representatives.Add(pack[0]);

        CardModel? pick = await CardSelectCmd.FromChooseACardScreen(choiceContext, representatives, player, canSkip);
        if (pick == null)
            return [];

        int idx = representatives.IndexOf(pick);
        if (idx < 0)
            return [];

        return bundles[idx].ToList();
    }

    private static int GetPackSlotCount(CardCreationOptions options, CardReward reward)
    {
        if (IsNeowBlessingStyleCardReward(options, reward))
            return NeowBlessingPackSlots;

        return options.RarityOdds switch
        {
            CardRarityOddsType.BossEncounter => 5,
            CardRarityOddsType.EliteEncounter => 5,
            CardRarityOddsType.RegularEncounter => RegularEncounterPackSlots(reward.Player),
            _ => Math.Clamp(GetOptionCount(reward), 3, 6),
        };
    }

    private static int RegularEncounterPackSlots(Player player)
    {
        int floorTier = Math.Min(2, (Math.Max(1, player.RunState.TotalFloor) - 1) / 6);
        return Math.Clamp(3 + floorTier, 3, 4);
    }

    private static int IndexOfBundleByInstanceSequence(
        List<IReadOnlyList<CardModel>> bundles,
        List<CardModel> chosen)
    {
        for (int i = 0; i < bundles.Count; i++)
        {
            IReadOnlyList<CardModel> b = bundles[i];
            if (b.Count != chosen.Count)
                continue;
            bool match = true;
            for (int j = 0; j < b.Count; j++)
            {
                if (!ReferenceEquals(b[j], chosen[j]))
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return i;
        }

        return -1;
    }

    private static void UnsubscribeRelicHandler(CardReward reward, Player player)
    {
        MethodInfo? onRelic = typeof(CardReward).GetMethod("OnRelicObtained", RewardMemberFlags);
        if (onRelic == null)
            return;

        var handler = (Action<RelicModel>)Delegate.CreateDelegate(typeof(Action<RelicModel>), reward, onRelic);
        player.RelicObtained -= handler;
    }

    private static CardCreationOptions GetCardCreationOptions(CardReward reward)
    {
        PropertyInfo? prop = typeof(CardReward).GetProperty("Options", RewardMemberFlags);
        if (prop?.GetValue(reward) is CardCreationOptions o)
            return o;
        throw new InvalidOperationException("CardReward.Options not accessible.");
    }

    private static int GetOptionCount(CardReward reward)
    {
        PropertyInfo? prop = typeof(CardReward).GetProperty("OptionCount", RewardMemberFlags);
        if (prop?.GetValue(reward) is int n)
            return n;
        throw new InvalidOperationException("CardReward.OptionCount not accessible.");
    }
}
