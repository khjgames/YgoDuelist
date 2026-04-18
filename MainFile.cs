using System;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using BaseLib.Utils;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YgoDuelist";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(YgoSaveTrunkSideMarkerNetPropertyNames));

        Harmony harmony = new(ModId);

        // PatchAll() with no assembly uses GetCallingAssembly(); the mod loader may not be YgoDuelist.dll,
        // so no patches from this mod would register. Always scan our assembly explicitly.
        harmony.PatchAll(typeof(MainFile).Assembly);

        YgoDuelistPower.PreloadShippedPowerTextures();

        // MP: set true while debugging NetCombatCard index desync (very verbose — every combat pile add).
        // YgoMpNetCardAssignLog.Verbose = true;

        // MP: extra GD.Print lines from YgoMpDiagnostics.VerbosePrint (pre-play grid pile membership, etc.).
        // YgoMpDiagnostics.Verbose = true;

        YgoMerchantShopBundlePurchasePatch.ApplyMerchantCardEntryPatches(harmony);

        MethodInfo? ancientSetInitial = AccessTools.DeclaredMethod(typeof(MegaCrit.Sts2.Core.Models.AncientEventModel), "SetInitialEventState");
        if (ancientSetInitial != null)
        {
            Patches patchInfo = Harmony.GetPatchInfo(ancientSetInitial);
            int prefixCount = patchInfo.Prefixes.Count;
            Logger.Info($"[YgoDuelist] Harmony AncientEventModel.SetInitialEventState prefix patches: {prefixCount}");
            if (prefixCount == 0)
                Logger.Warn("[YgoDuelist] No prefixes on SetInitialEventState — Neow starter draft patch may not be applied.");
        }
        else
            Logger.Warn("[YgoDuelist] Could not resolve AncientEventModel.SetInitialEventState for patch diagnostics.");

        harmony.Patch(
            AccessTools.Method(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals)),
            prefix: new HarmonyMethod(typeof(StaticImageCreateVisualsPatch), nameof(StaticImageCreateVisualsPatch.Prefix)));

        ModHelper.AddModelToPool<YgoDuelistRelicPool, GraveyardRelic>();
        ModHelper.AddModelToPool<YgoDuelistRelicPool, BanishedRelic>();
        ModHelper.AddModelToPool<YgoDuelistRelicPool, ExtraDeckRelic>();
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