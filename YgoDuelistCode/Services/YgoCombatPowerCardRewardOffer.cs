using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Optional combat reward: one <see cref="BaseYgoPowerCard"/> via vanilla <see cref="CardReward"/> (take or skip).
/// Rolled once per combat when rewards are generated.
/// </summary>
public static class YgoCombatPowerCardRewardOffer
{
    public const float OfferChance = 0.15f;

    private static readonly object PowerBonusRewardTag = new();

    private static readonly ConditionalWeakTable<CardReward, object> TaggedPowerBonusRewards = new();

    public static bool IsPowerCardBonusReward(CardReward reward) =>
        TaggedPowerBonusRewards.TryGetValue(reward, out _);

    public static CardReward? TryCreateIfRolled(Player player, RoomType roomType)
    {
        if (YgoPowerCardCatalog.GetUnlockedPowerCardTemplates(player).Count == 0)
            return null;

        Rng rng = player.PlayerRng.Rewards;
        float roll = rng.NextFloat();
        if (roll >= OfferChance)
        {
            Log.Info(
                $"[YgoDuelist][PowerCardReward] skipped | room={roomType} | roll={roll:F4} | threshold={OfferChance:F2}");
            return null;
        }

        CardReward reward = Create(player, roomType);
        Log.Info(
            $"[YgoDuelist][PowerCardReward] offered | room={roomType} | roll={roll:F4} | rarityOdds={GetRarityOdds(roomType)}");
        return reward;
    }

    private static CardReward Create(Player player, RoomType roomType)
    {
        CardRarityOddsType rarityOdds = GetRarityOdds(roomType);
        var options = new CardCreationOptions(
            [player.Character.CardPool],
            CardCreationSource.Encounter,
            rarityOdds,
            static c => c is BaseYgoPowerCard);

        var reward = new CardReward(options, cardCount: 1, player);
        TaggedPowerBonusRewards.Add(reward, PowerBonusRewardTag);
        return reward;
    }

    private static CardRarityOddsType GetRarityOdds(RoomType roomType) =>
        roomType switch
        {
            RoomType.Elite => CardRarityOddsType.EliteEncounter,
            RoomType.Boss => CardRarityOddsType.BossEncounter,
            _ => CardRarityOddsType.RegularEncounter,
        };
}
