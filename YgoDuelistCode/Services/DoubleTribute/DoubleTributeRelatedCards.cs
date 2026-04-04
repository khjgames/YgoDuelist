using System;
using System.Collections.Generic;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class DoubleTributeRelatedCards
{
    private static readonly object CacheGate = new();
    private static readonly Dictionary<Type, Type[]> Cache = new();

    /// <summary>
    /// Self, other double summoners sharing this card's attribute or race on the field material,
    /// and level 7+ normal/effect monsters this material counts double toward.
    /// </summary>
    public static Type[] For(Type selfType, DoubleTributeSummonTargetSpec targetSpec)
    {
        lock (CacheGate)
        {
            if (Cache.TryGetValue(selfType, out Type[]? cached))
                return cached;

            var set = new HashSet<Type> { selfType };

            foreach (Type other in DoubleTributeMaterialCatalog.DoubleTributeMaterialCardTypes)
            {
                if (other != selfType && DoubleTributeMaterialCatalog.ShareMaterialGroup(selfType, other))
                    set.Add(other);
            }

            foreach (Type t in DoubleTributeMonsterCatalogCache.EnumerateTypesMatching(targetSpec))
                set.Add(t);

            var arr = new Type[set.Count];
            set.CopyTo(arr);
            Array.Sort(arr, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            Cache[selfType] = arr;
            return arr;
        }
    }
}
