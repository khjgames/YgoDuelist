using System;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using BaseLib.Utils;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YgoDuelist";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();

        ModHelper.AddModelToPool<YgoDuelistRelicPool, GraveyardRelic>();
        ModHelper.AddModelToPool<YgoDuelistRelicPool, CardOptionsRelic>();

        RegisterAllYgoCards();

        // Prewarm pool for the ZGO option-hand holders so NodePool.Get<NYgoOptionCardHolder>()
        // is valid when the option UI first appears.
        GeneratedNodePool.Init(NYgoOptionCardHolder.NewInstanceForPool, 8);
    }

    private static void RegisterAllYgoCards()
    {
        MethodInfo? addToPoolMethod = typeof(ModHelper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == nameof(ModHelper.AddModelToPool) &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 2 &&
                method.GetParameters().Length == 0);

        if (addToPoolMethod == null)
            throw new InvalidOperationException("Unable to locate ModHelper.AddModelToPool<TPool, TModel>.");

        var cardTypes = typeof(MainFile).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsClass: true } &&
                type.Namespace != null &&
                type.Namespace.StartsWith("YgoDuelist.YgoDuelistCode.Cards.", StringComparison.Ordinal) &&
                !type.Namespace.Contains(".Command", StringComparison.Ordinal) &&
                typeof(YgoDuelistCard).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (Type cardType in cardTypes)
        {
            MethodInfo generic = addToPoolMethod.MakeGenericMethod(typeof(YgoDuelistCardPool), cardType);
            generic.Invoke(null, null);
        }
    }
}