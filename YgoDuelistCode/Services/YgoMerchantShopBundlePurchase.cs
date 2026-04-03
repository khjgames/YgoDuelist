using System;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
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
    private static readonly FieldInfo? MerchantEntryPlayerField =
        AccessTools.DeclaredField(typeof(MerchantEntry), "_player");

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

        if (!YgoMerchantShopBundleShared.TryGetBundlingTemplate(cr.Card, out YgoDuelistCard y))
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("ScheduleGrantFromEntry: abort TryGetBundlingTemplate false");
            return;
        }

        if (y.BundledCards.Length == 0)
        {
            if (diag)
                YgoMerchantShopBundleDiag.Log("ScheduleGrantFromEntry: abort BundledCards empty on template");
            return;
        }

        Player? player = MerchantEntryPlayerField?.GetValue(entry) as Player;
        if (player == null)
        {
            YgoMerchantShopBundleDiag.Log($"ScheduleGrantFromEntry: abort _player null offer={offerId}");
            return;
        }

        ModelId mainId = cr.Card.CanonicalInstance.Id;
        Type[] types = y.BundledCards.ToArray();
        bool extraSelf = y.BundleGrantsExtraCopyOfSelf;
        YgoMerchantShopBundleDiag.Log(
            $"ScheduleGrantFromEntry: deferred GrantNow offer={offerId} mainCanon={mainId.Entry} bundleTypes={string.Join(",", types.Select(t => t.Name))} extraSelf={extraSelf}");

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
            GrantBundledCardsBlocking(player, types, mainId, extraSelf, offerId);
        }).CallDeferred();
    }

    /// <summary>
    /// Runs deferred on the main thread so <see cref="CardPileCmd.Add"/> is not invoked from inside merchant purchase continuations.
    /// </summary>
    private static void GrantBundledCardsBlocking(
        Player player,
        Type[] bundleTypes,
        ModelId purchasedCanonicalId,
        bool bundleGrantsExtraCopyOfSelf,
        string offerIdForLog)
    {
        try
        {
            YgoMerchantBuyGridScaleTrace.Log(
                $"GrantBundledCardsBlocking START (deferred; may run after shop UI updates) offer={offerIdForLog}");
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
                    RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedCard(instance);
            }

            YgoMerchantShopBundleDiag.Log($"GrantNow: END offer={offerIdForLog}");
            YgoMerchantBuyGridScaleTrace.Log($"GrantBundledCardsBlocking END offer={offerIdForLog}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[YgoDuelist][ShopBundle] GrantNow FATAL offer={offerIdForLog} {ex}");
        }
    }
}
