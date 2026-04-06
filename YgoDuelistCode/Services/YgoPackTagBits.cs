using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Bit helpers for sealed pack visuals (tag order matches ascending bit index).</summary>
public static class YgoPackTagBits
{
    /// <summary>Single-bit flags in <paramref name="mask"/>, lowest bit first.</summary>
    public static List<YgoCardPackTags> EnumerateSingleBitsOrdered(YgoCardPackTags mask)
    {
        var list = new List<YgoCardPackTags>(3);
        long m = (long)mask;
        for (int i = 0; i < 64; i++)
        {
            long bit = 1L << i;
            if ((m & bit) != 0)
                list.Add((YgoCardPackTags)bit);
        }

        return list;
    }

    public static int PopCount(YgoCardPackTags mask) => EnumerateSingleBitsOrdered(mask).Count;
}
