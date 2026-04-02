using System.Collections.Generic;
using System.Linq;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Single-bit pack themes that participate in run-long fatigue (main + sub roll lists, unique).
/// </summary>
public static class YgoPackParticipatingTags
{
    public static readonly long[] AllFlagValues;

    static YgoPackParticipatingTags()
    {
        var set = new HashSet<long>();
        foreach (YgoCardPackTags t in YgoPackCardCatalog.PackThemeMainTags)
            set.Add((long)t);
        foreach (YgoCardPackTags t in YgoPackCardCatalog.PackThemeSubTags)
            set.Add((long)t);
        AllFlagValues = set.OrderBy(x => x).ToArray();
    }
}
