using System;
using System.Collections.Generic;
using System.Reflection;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Finds all <see cref="IDoubleTributeMaterial"/> <see cref="BaseMonsterCard"/> types for related-card grouping.
/// Per-card tribute rules live on each card; nothing is listed here.
/// </summary>
public static class DoubleTributeMaterialCatalog
{
    private static readonly object Gate = new();
    private static Type[]? _types;
    private static readonly Dictionary<Type, BaseMonsterCard?> PrototypeCache = new();

    public static IReadOnlyList<Type> DoubleTributeMaterialCardTypes
    {
        get
        {
            EnsureTypes();
            return _types!;
        }
    }

    private static void EnsureTypes()
    {
        lock (Gate)
        {
            if (_types != null)
                return;

            var list = new List<Type>();
            Assembly asm = typeof(IDoubleTributeMaterial).Assembly;
            foreach (Type t in asm.GetTypes())
            {
                if (!t.IsClass || t.IsAbstract)
                    continue;
                if (!typeof(BaseMonsterCard).IsAssignableFrom(t))
                    continue;
                if (!typeof(IDoubleTributeMaterial).IsAssignableFrom(t))
                    continue;
                list.Add(t);
            }

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            _types = list.ToArray();
        }
    }

    internal static BaseMonsterCard? TryPrototype(Type cardType)
    {
        lock (Gate)
        {
            if (PrototypeCache.TryGetValue(cardType, out BaseMonsterCard? hit))
                return hit;

            BaseMonsterCard? model;
            try
            {
                model = YgoPackCardCatalog.CardFromType(cardType) as BaseMonsterCard;
            }
            catch
            {
                model = null;
            }

            PrototypeCache[cardType] = model;
            return model;
        }
    }

    /// <summary>
    /// Double-tribute material monsters whose <see cref="IDoubleTributeMaterial.DoubleTributeTargetSpec"/> matches
    /// <paramref name="summon"/> (level 7+ normal/effect tribute targets).
    /// </summary>
    public static IEnumerable<Type> EnumerateMaterialTypesMatchingSummonTarget(BaseMonsterCard summon)
    {
        foreach (Type t in DoubleTributeMaterialCardTypes)
        {
            BaseMonsterCard? proto = TryPrototype(t);
            if (proto is IDoubleTributeMaterial m && m.DoubleTributeTargetSpec.Matches(summon))
                yield return t;
        }
    }

    /// <summary>True when both are double-tribute materials and share printed attribute or race.</summary>
    public static bool ShareMaterialGroup(Type a, Type b)
    {
        if (a == b)
            return true;

        BaseMonsterCard? pa = TryPrototype(a);
        BaseMonsterCard? pb = TryPrototype(b);
        if (pa == null || pb == null)
            return false;

        return pa.DuelMonsterAttribute == pb.DuelMonsterAttribute || pa.DuelMonsterRace == pb.DuelMonsterRace;
    }
}
