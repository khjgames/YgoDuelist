using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Cards with <see cref="YgoCardPackTags.Starter"/> for the pre-Neow starter grid.
/// </summary>
public static class YgoStarterCardCatalog
{
    private static readonly object Gate = new();
    private static List<Type>? sStarterCardTypes;
    private static MethodInfo? sModelDbCardNoArg;

    public static List<CardModel> CreateRandomGrid(Rng rng, int count)
    {
        IReadOnlyList<Type> types = GetStarterCardTypes();
        GD.Print($"[YgoDuelist NeowDraft] CreateRandomGrid: starterTagTypeCount={types.Count} requestedCount={count}");
        MainFile.Logger.Info($"[NeowDraft catalog] starterTagTypeCount={types.Count} requestedCount={count}");
        if (types.Count == 0)
            throw new InvalidOperationException("No YgoDuelistCard types with YgoCardPackTags.Starter; add Starter to PackTags or the Neow grid cannot run.");
        if (types.Count < count)
            throw new InvalidOperationException($"Need at least {count} distinct Starter-tagged card types; only {types.Count} available.");

        var pool = types.ToList();
        pool.UnstableShuffle(rng);

        var grid = new List<CardModel>(count);
        for (int i = 0; i < count; i++)
        {
            Type t = pool[i];
            CardModel canonical = CardFromType(t);
            CardModel mutable = canonical.ToMutable();
            mutable.FloorAddedToDeck = 1;
            grid.Add(mutable);
        }

        return grid;
    }

    private static IReadOnlyList<Type> GetStarterCardTypes()
    {
        lock (Gate)
        {
            if (sStarterCardTypes != null)
                return sStarterCardTypes;

            var list = new List<Type>();
            foreach (Type t in typeof(YgoDuelistCard).Assembly.GetTypes())
            {
                if (t.IsAbstract || !t.IsSubclassOf(typeof(YgoDuelistCard)))
                    continue;

                CardModel model;
                try
                {
                    model = CardFromType(t);
                }
                catch
                {
                    continue;
                }

                if (model is YgoDuelistCard ygo && (ygo.PackTags & YgoCardPackTags.Starter) != 0)
                    list.Add(t);
            }

            sStarterCardTypes = list;
            GD.Print($"[YgoDuelist NeowDraft] catalog built: {list.Count} concrete YgoDuelistCard types with PackTags.Starter");
            MainFile.Logger.Info($"[NeowDraft catalog] built {list.Count} starter-tagged card types");
            return sStarterCardTypes;
        }
    }

    private static CardModel CardFromType(Type cardType)
    {
        sModelDbCardNoArg ??= typeof(ModelDb)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(ModelDb.Card) && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 0);

        MethodInfo closed = sModelDbCardNoArg.MakeGenericMethod(cardType);
        return (CardModel)closed.Invoke(null, null)!;
    }
}
