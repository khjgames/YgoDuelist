using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Patches;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// When a YGO merchant slot sells a <see cref="YgoDuelistCard"/> with <see cref="YgoDuelistCard.BundledCards"/> and/or
/// <see cref="YgoDuelistCard.BulkBundled"/>, the matching templates are added to the deck after the main purchase succeeds.
/// Bundled entries matching the purchased card are skipped unless <see cref="YgoDuelistCard.BundleGrantsExtraCopyOfSelf"/> is true (2-of-self bundles).
/// </summary>
public static class YgoMerchantShopBundlePurchase
{
    /// <summary>Runs before <see cref="MerchantCardEntry.ClearAfterPurchase"/> or <see cref="MerchantCardEntry.RestockAfterPurchase"/> clears the sold card from the entry.</summary>
    public static void ScheduleGrantFromEntry(MerchantCardEntry entry)
    {
        CardCreationResult? cr = entry.CreationResult;
        string offerId = cr?.Card?.Id.Entry ?? "(no card)";
        bool diag = YgoMerchantShopBundleDiag.IsMaskedBeastDiagCard(cr?.Card);
        if (diag)
            YgoMerchantShopBundleDiag.Log($"ScheduleGrantFromEntry: begin offer={offerId} crNull={cr == null}");

        if (cr?.Card == null)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("ScheduleGrantFromEntry: abort CreationResult or Card null");
            return;
        }

        Player? player = YgoMerchantShopBundleShared.GetMerchantEntryPlayer(entry);
        if (player == null)
        {
            YgoMerchantShopBundleDiag.Log($"ScheduleGrantFromEntry: abort _player null offer={offerId}");
            return;
        }

        if (YgoPlayerRunPiles.IsYgoRunPlayer(player))
        {
            YgoPlayerMinimumDeck.AddReceivedCardsFromPacksOrShop(player, 1);
            YgoTopBarDeckCountTextPatch.RefreshDeckCountLabelForPlayer(player);
        }

        if (!YgoMerchantShopBundleShared.TryGetBundlingTemplate(cr.Card, out YgoDuelistCard y))
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("ScheduleGrantFromEntry: abort TryGetBundlingTemplate false");
            return;
        }

        bool grantExplicitBundle = y.BundledCards.Length > 0;
        bool grantBulk = y.BulkBundled;
        if (!grantExplicitBundle && !grantBulk)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("ScheduleGrantFromEntry: abort no BundledCards and not BulkBundled");
            return;
        }

        YgoCardPackTags rowTagMask = YgoMerchantShopBundleShared.TryGetEntryTagMask(entry, out YgoCardPackTags mask)
            ? mask
            : YgoCardPackTags.None;

        ModelId mainId = cr.Card.CanonicalInstance.Id;
        Type[] types = y.BundledCards.ToArray();
        bool extraSelf = y.BundleGrantsExtraCopyOfSelf;
        YgoMerchantShopBundleDiag.Log(
            $"ScheduleGrantFromEntry: deferred GrantNow offer={offerId} mainCanon={mainId.Entry} bundleTypes={string.Join(",", types.Select(t => t.Name))} extraSelf={extraSelf} grantBulk={grantBulk}");

        NGame? root = NGame.Instance;
        if (root == null || !GodotObject.IsInstanceValid(root))
        {
            MainFile.Logger.Error($"[YgoDuelist][ShopBundle] ScheduleGrantFromEntry: NGame.Instance missing offer={offerId}");
            return;
        }

        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(NGame.Instance))
                return;
            int grantedExtras = 0;
            if (grantExplicitBundle)
                grantedExtras += GrantBundledCardsBlocking(player, types, mainId, extraSelf, offerId);
            if (grantBulk
                && YgoBulkBundledResolver.TryGetMerchantBulkMateTemplate(entry, player, y, rowTagMask, out CardModel? bulkTemplate)
                && bulkTemplate != null)
                grantedExtras += GrantSingleTemplateBlocking(player, bulkTemplate, offerId, "bulkBundled");
            if (grantedExtras > 0 && YgoPlayerRunPiles.IsYgoRunPlayer(player))
            {
                YgoPlayerMinimumDeck.AddReceivedCardsFromPacksOrShop(player, grantedExtras);
                YgoTopBarDeckCountTextPatch.RefreshDeckCountLabelForPlayer(player);
            }
        }).CallDeferred();
    }

    private static int GrantSingleTemplateBlocking(Player player, CardModel template, string offerIdForLog, string reason)
    {
        try
        {
            YgoMerchantShopBundleDiag.Log($"GrantNow: {reason} template={template.Id.Entry} offer={offerIdForLog}");
            CardModel instance = player.RunState.CreateCard(template, player);
            CardPileAddResult result = CardPileCmd.Add(instance, PileType.Deck).GetAwaiter().GetResult();
            if (!result.success)
            {
                MainFile.Logger.Warn(
                    $"[YgoDuelist][ShopBundle] {reason} card not added offer={offerIdForLog} template={template.Id.Entry}");
            }
            else
            {
                RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedCard(instance);
                return 1;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[YgoDuelist][ShopBundle] GrantSingleTemplateBlocking FATAL offer={offerIdForLog} {ex}");
        }

        return 0;
    }

    /// <summary>
    /// Runs deferred on the main thread so <see cref="CardPileCmd.Add"/> is not invoked from inside merchant purchase continuations.
    /// </summary>
    private static int GrantBundledCardsBlocking(
        Player player,
        Type[] bundleTypes,
        ModelId purchasedCanonicalId,
        bool bundleGrantsExtraCopyOfSelf,
        string offerIdForLog)
    {
        int granted = 0;
        try
        {
            YgoMerchantShopBundleDiag.Log(
                $"GrantNow: START offer={offerIdForLog} purchasedCanon={purchasedCanonicalId.Entry} count={bundleTypes.Length}");
            foreach (Type bt in bundleTypes)
            {
                CardModel template;
                try
                {
                    template = YgoPackCardCatalog.CardFromType(bt);
                }
                catch (Exception ex)
                {
                    YgoMerchantShopBundleDiag.Log($"GrantNow: CardFromType EX type={bt.FullName} msg={ex.Message}");
                    continue;
                }

                if (template.Id == purchasedCanonicalId && !bundleGrantsExtraCopyOfSelf)
                {
                    YgoMerchantShopBundleDiag.Log(
                        $"GrantNow: skip same-as-purchase template={template.Id.Entry} extraSelf={bundleGrantsExtraCopyOfSelf}");
                    continue;
                }

                YgoMerchantShopBundleDiag.Log($"GrantNow: adding template={template.Id.Entry} from bundle type={bt.Name}");
                CardModel instance = player.RunState.CreateCard(template, player);
                CardPileAddResult result = CardPileCmd.Add(instance, PileType.Deck).GetAwaiter().GetResult();
                YgoMerchantShopBundleDiag.Log($"GrantNow: CardPileCmd.Add template={template.Id.Entry} success={result.success}");
                if (!result.success)
                {
                    MainFile.Logger.Warn(
                        $"[YgoDuelist][ShopBundle] Bundle card not added to deck (vanilla Hook.ShouldAddToDeck returned false or pile rules failed). template={template.Id.Entry} offer={offerIdForLog}");
                }
                else
                {
                    RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedCard(instance);
                    granted++;
                }
            }

            YgoMerchantShopBundleDiag.Log($"GrantNow: END offer={offerIdForLog}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[YgoDuelist][ShopBundle] GrantNow FATAL offer={offerIdForLog} {ex}");
        }

        return granted;
    }
}
