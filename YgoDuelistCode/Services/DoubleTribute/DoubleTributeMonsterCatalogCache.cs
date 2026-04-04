using System;
using System.Collections.Generic;
using System.Reflection;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Caches level 7+ normal/effect monsters (non-ritual, non-fusion) for double-tribute related cards.</summary>
internal static class DoubleTributeMonsterCatalogCache
{
    private static readonly object Gate = new();
    private static List<BaseMonsterCard>? _level7PlusPrototypes;

    public static IEnumerable<Type> EnumerateTypesMatching(DoubleTributeSummonTargetSpec spec)
    {
        Ensure();
        foreach (BaseMonsterCard proto in _level7PlusPrototypes!)
        {
            if (spec.Matches(proto))
                yield return proto.GetType();
        }
    }

    private static void Ensure()
    {
        lock (Gate)
        {
            if (_level7PlusPrototypes != null)
                return;

            var list = new List<BaseMonsterCard>();
            Assembly asm = typeof(IDoubleTributeMaterial).Assembly;
            foreach (Type t in asm.GetTypes())
            {
                if (!t.IsClass || t.IsAbstract || !typeof(BaseMonsterCard).IsAssignableFrom(t))
                    continue;

                BaseMonsterCard? c = TryCreate(t);
                if (c == null)
                    continue;
                if (c.YgoCardType == YgoCardType.FusionMonster || c.YgoCardType == YgoCardType.RitualMonster)
                    continue;
                if (c.YgoCardType != YgoCardType.Monster && c.YgoCardType != YgoCardType.EffectMonster)
                    continue;
                if (c.GetEffectiveDuelMonsterLevel() < 7)
                    continue;
                list.Add(c);
            }

            _level7PlusPrototypes = list;
        }
    }

    private static BaseMonsterCard? TryCreate(Type t)
    {
        try
        {
            return Activator.CreateInstance(t) as BaseMonsterCard;
        }
        catch
        {
            return null;
        }
    }
}
