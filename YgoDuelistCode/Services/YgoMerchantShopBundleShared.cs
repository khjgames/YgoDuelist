using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shop <see cref="CardModel"/> instances are often mutable copies; <see cref="YgoDuelistCard.BundledCards"/> lives on the template.
/// </summary>
public static class YgoMerchantShopBundleShared
{
    public static bool TryGetBundlingTemplate(CardModel? card, out YgoDuelistCard ygo)
    {
        ygo = null!;
        if (card == null)
            return false;

        if (card is YgoDuelistCard direct)
        {
            ygo = direct;
            return true;
        }

        if (card.CanonicalInstance is YgoDuelistCard canon)
        {
            ygo = canon;
            return true;
        }

        return false;
    }
}
