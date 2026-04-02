using System.Collections.Generic;
using System.Globalization;
using System.Text;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoPackTagBalanceSerializer
{
    public const int MaxSerializedAccumPerTag = 65535;

    public static string Serialize(IReadOnlyDictionary<long, int> accumByFlag)
    {
        var sb = new StringBuilder();
        foreach (long bit in YgoPackParticipatingTags.AllFlagValues)
        {
            int v = accumByFlag.TryGetValue(bit, out int x) ? x : 0;
            v = Math.Clamp(v, 0, MaxSerializedAccumPerTag);
            if (sb.Length > 0)
                sb.Append(',');
            sb.Append(CultureInfo.InvariantCulture, $"{bit}:{v}");
        }

        return sb.ToString();
    }

    public static void ParseInto(string? blob, Dictionary<long, int> target)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(blob))
            return;

        foreach (string part in blob.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int colon = part.IndexOf(':');
            if (colon <= 0)
                continue;
            if (!long.TryParse(part.AsSpan(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out long bit))
                continue;
            if (!int.TryParse(part.AsSpan(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                continue;
            if (!IsParticipatingBit(bit))
                continue;
            target[bit] = Math.Clamp(v, 0, MaxSerializedAccumPerTag);
        }
    }

    private static bool IsParticipatingBit(long bit)
    {
        foreach (long p in YgoPackParticipatingTags.AllFlagValues)
        {
            if (p == bit)
                return true;
        }

        return false;
    }

    public static bool HasAnyNonZeroAccum(IReadOnlyDictionary<long, int> accumByFlag)
    {
        foreach (KeyValuePair<long, int> kv in accumByFlag)
        {
            if (kv.Value != 0)
                return true;
        }

        return false;
    }
}
