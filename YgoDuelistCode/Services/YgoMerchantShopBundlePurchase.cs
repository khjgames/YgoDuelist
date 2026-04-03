using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// When a YGO merchant slot sells a <see cref="YgoDuelistCard"/> with <see cref="YgoDuelistCard.BundledCards"/>,
/// the listed templates are added to the deck after the main purchase succeeds. Entries matching the purchased card
/// are skipped unless <see cref="YgoDuelistCard.BundleGrantsExtraCopyOfSelf"/> is true (2-of-self bundles).
/// </summary>
public static class YgoMerchantShopBundlePurchase
{
    /// <summary>Runs before <see cref="MerchantCardEntry.ClearAfterPurchase"/> or <see cref="MerchantCardEntry.RestockAfterPurchase"/> clears the sold card from the entry.</summary>
    public static void ScheduleGrantFromEntry(MerchantCardEntry entry)
    {
        CardCreationResult? cr = entry.CreationResult;
        if (cr?.Card == null
            || !YgoMerchantShopBundleShared.TryGetBundlingTemplate(cr.Card, out YgoDuelistCard y)
            || y.BundledCards.Length == 0)
            return;

        Player? player = Traverse.Create(entry).Field<Player>("_player").Value;
        if (player == null)
            return;

        ModelId mainId = cr.Card.CanonicalInstance.Id;
        Type[] types = y.BundledCards.ToArray();
        bool extraSelf = y.BundleGrantsExtraCopyOfSelf;
        TaskHelper.RunSafely(GrantAsync(player, types, mainId, extraSelf));
    }

    private static async Task GrantAsync(Player player, Type[] bundleTypes, ModelId purchasedCanonicalId, bool bundleGrantsExtraCopyOfSelf)
    {
        foreach (Type bt in bundleTypes)
        {
            CardModel template;
            try
            {
                template = YgoPackCardCatalog.CardFromType(bt);
            }
            catch
            {
                continue;
            }

            if (template.Id == purchasedCanonicalId && !bundleGrantsExtraCopyOfSelf)
                continue;

            CardModel instance = player.RunState.CreateCard(template, player);
            var result = await CardPileCmd.Add(instance, PileType.Deck);
            if (result.success)
                RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedCard(instance);
        }
    }
}
