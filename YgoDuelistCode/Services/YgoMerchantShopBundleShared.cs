using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shop <see cref="CardModel"/> instances are often mutable copies; <see cref="YgoDuelistCard.BundledCards"/> lives on the template.
/// </summary>
public static class YgoMerchantShopBundleShared
{
    private static readonly FieldInfo? MerchantEntryPlayerField =
        AccessTools.DeclaredField(typeof(MerchantEntry), "_player");

    public static Player? GetMerchantEntryPlayer(MerchantEntry entry) =>
        MerchantEntryPlayerField?.GetValue(entry) as Player;

    public static bool TryGetBundlingTemplate(CardModel? card, out YgoDuelistCard ygo)
    {
        ygo = null!;
        if (card == null)
            return false;

        bool diag = YgoMerchantShopBundleDiag.IsMaskedBeastDiagCard(card);

        if (card is YgoDuelistCard direct)
        {
            ygo = direct;
            if (diag)
                YgoMerchantShopBundleDiag.Log(
                    $"TryGetBundlingTemplate: OK direct YgoDuelistCard type={direct.GetType().FullName} bundled={direct.BundledCards.Length}");
            return true;
        }

        CardModel canon = card.CanonicalInstance;
        if (canon is YgoDuelistCard canonYgo)
        {
            ygo = canonYgo;
            if (diag)
                YgoMerchantShopBundleDiag.Log(
                    $"TryGetBundlingTemplate: OK via CanonicalInstance cardClr={card.GetType().FullName} canonClr={canon.GetType().FullName} bundled={canonYgo.BundledCards.Length}");
            return true;
        }

        if (diag)
            YgoMerchantShopBundleDiag.Log(
                $"TryGetBundlingTemplate: FAIL cardClr={card.GetType().FullName} canonClr={canon.GetType().FullName} canonIsYgo=False");
        return false;
    }
}
