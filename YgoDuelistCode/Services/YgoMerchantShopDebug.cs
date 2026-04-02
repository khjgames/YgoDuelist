using System.Collections.Generic;
using System.Text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Verbose shop layout logs for verifying UI vs pack logic.</summary>
public static class YgoMerchantShopDebug
{
    public static void LogOfferLayout(YgoMerchantOfferGenerator.ShopOffer offer)
    {
        IReadOnlyList<YgoCardPackTags> masks = offer.PackMasks;
        var sb = new StringBuilder();
        sb.AppendLine("[YgoDuelist][Shop][Layout] Rolled pack tag masks (cards in that pack are weighted by these tags):");
        if (masks.Count > 0)
            sb.AppendLine($"  DoubleFirst (5 cells): {ExpandPackTags(masks[0])}");
        if (masks.Count > 1)
            sb.AppendLine($"  DoubleSecond (5 cells): {ExpandPackTags(masks[1])}");
        if (masks.Count > 2)
            sb.AppendLine($"  SingleTagFirst (4 cells): {ExpandPackTags(masks[2])}");
        if (masks.Count > 3)
            sb.AppendLine($"  SingleTagSecond (4 cells): {ExpandPackTags(masks[3])}");

        sb.AppendLine("[YgoDuelist][Shop][Layout] Per-cell (list index = grid row-major 0..17, col = idx%6 row=idx/6):");
        for (int i = 0; i < offer.Slots.Count; i++)
        {
            YgoMerchantOfferGenerator.ShopSlot s = offer.Slots[i];
            int row = i / YgoMerchantOfferGenerator.GridColumns;
            int col = i % YgoMerchantOfferGenerator.GridColumns;
            string cardTags = s.Template is YgoDuelistCard y ? ExpandPackTags(y.PackTags) : "(not YgoDuelistCard)";
            string frame = s.Template is IYgoCard iy ? iy.YgoCardType.ToString() : "n/a";
            sb.AppendLine(
                $"  idx={i} grid=({row},{col}) pack={s.PackRole} offeredRarity={s.Rarity} templateId={s.Template.Id} " +
                $"templateRarity={s.Template.Rarity} cellPackMask={ExpandPackTags(s.RowTagMask)} cardPackTags={cardTags} ygoFrame={frame}");
        }

        Log.Info(sb.ToString().TrimEnd());
    }

    public static string ExpandPackTags(YgoCardPackTags mask)
    {
        if (mask == YgoCardPackTags.None)
            return "None";

        var parts = new List<string>();
        foreach (YgoCardPackTags bit in YgoPackCardCatalog.PackThemeMainTags)
        {
            if ((mask & bit) != 0)
                parts.Add(bit.ToString());
        }

        foreach (YgoCardPackTags bit in YgoPackCardCatalog.PackThemeSubTags)
        {
            if ((mask & bit) != 0)
                parts.Add(bit.ToString());
        }

        return parts.Count > 0 ? string.Join(", ", parts) : mask.ToString();
    }
}
