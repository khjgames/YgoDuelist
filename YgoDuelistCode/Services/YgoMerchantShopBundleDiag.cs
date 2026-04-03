using MegaCrit.Sts2.Core.Models;
using YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Verbose <see cref="MainFile.Logger"/> lines for merchant bundle troubleshooting (grep <c>[YgoDuelist][ShopBundle]</c>).</summary>
public static class YgoMerchantShopBundleDiag
{
    public const string Tag = "[YgoDuelist][ShopBundle]";

    public static bool IsMaskedBeastDiagCard(CardModel? card) =>
        card != null
        && card.Id.Entry.Contains("THE_MASKED_BEAST", System.StringComparison.OrdinalIgnoreCase);

    public static void Log(string message) =>
        MainFile.Logger.Info($"{Tag} {message}");
}
