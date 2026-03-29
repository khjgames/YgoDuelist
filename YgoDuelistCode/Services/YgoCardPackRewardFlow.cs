using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YgoDuelist card reward: three packs via <see cref="CardSelectCmd.FromChooseABundleScreen"/>, then deck adds + run history/sync (vanilla <see cref="CardReward.OnSelect"/> responsibilities).
/// </summary>
public static class YgoCardPackRewardFlow
{
    private static readonly BindingFlags RewardMemberFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static bool ShouldReplaceCardRewardSelection(CardReward reward)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(reward.Player))
            return false;
        CardCreationOptions options = GetCardCreationOptions(reward);
        if (options.Source != CardCreationSource.Encounter)
            return false;
        if (options.RarityOdds == CardRarityOddsType.BossEncounter)
            return false;
        return true;
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
        int slotCount = GetOptionCount(reward);

        Rng rng = player.PlayerRng.Rewards;
        List<List<CardModel>> templatePacks = YgoCardPackGenerator.GenerateThreePackTemplates(
            player,
            rng,
            slotCount,
            options.RarityOdds);

        var bundles = new List<IReadOnlyList<CardModel>>(3);
        foreach (List<CardModel> pack in templatePacks)
        {
            var row = new List<CardModel>(pack.Count);
            foreach (CardModel template in pack)
            {
                row.Add(player.RunState.CreateCard(template, player));
            }

            bundles.Add(row);
        }

        IEnumerable<CardModel> chosenEnumerable = await CardSelectCmd.FromChooseABundleScreen(player, bundles);
        List<CardModel> chosenCards = chosenEnumerable.ToList();

        UnsubscribeRelicHandler(reward, player);

        int chosenIndex = IndexOfBundleByInstanceSequence(bundles, chosenCards);
        var history = player.RunState.CurrentMapPointHistoryEntry.GetEntry(LocalContext.NetId!.Value);

        foreach (CardModel card in chosenCards)
        {
            CardPileAddResult add = await CardPileCmd.Add(card, PileType.Deck);
            if (!add.success)
                continue;

            CardModel added = add.cardAdded;
            Log.Info($"[YgoDuelist] Pack reward obtained {added.Id}");
            RunManager.Instance!.RewardSynchronizer.SyncLocalObtainedCard(added);
            history.CardChoices.Add(new CardChoiceHistoryEntry(added, wasPicked: true));
        }

        if (chosenIndex >= 0)
        {
            for (int i = 0; i < bundles.Count; i++)
            {
                if (i == chosenIndex)
                    continue;
                foreach (CardModel c in bundles[i])
                {
                    history.CardChoices.Add(new CardChoiceHistoryEntry(c, wasPicked: false));
                    RunManager.Instance!.RewardSynchronizer.SyncLocalSkippedCard(c);
                }
            }
        }
        else if (chosenCards.Count == 0)
        {
            foreach (IReadOnlyList<CardModel> b in bundles)
            {
                foreach (CardModel c in b)
                {
                    history.CardChoices.Add(new CardChoiceHistoryEntry(c, wasPicked: false));
                    RunManager.Instance!.RewardSynchronizer.SyncLocalSkippedCard(c);
                }
            }
        }

        return false;
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
