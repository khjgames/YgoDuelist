using System;
using System.Collections.Generic;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Ordering helpers for <see cref="YgoPreviewReferencedCardTypes"/> — keeps generated preview map file untouched.
/// </summary>
public static class YgoPreviewReferenceOrder
{
    /// <summary>
    /// Emits <paramref name="leading"/> in order first, then remaining types from
    /// <see cref="YgoPreviewReferencedCardTypes.Merged"/> (localization quotes + optional extras), deduped.
    /// </summary>
    public static Type[] LeadingThenMerged(Type hostCardType, Type[] leading, params Type[]? mergedExtras)
    {
        var seen = new HashSet<Type>();
        var list = new List<Type>();
        foreach (Type? t in leading)
        {
            if (t != null && t != hostCardType && seen.Add(t))
                list.Add(t);
        }

        foreach (Type? t in YgoPreviewReferencedCardTypes.Merged(hostCardType, mergedExtras))
        {
            if (t != null && t != hostCardType && seen.Add(t))
                list.Add(t);
        }

        return list.ToArray();
    }
}
