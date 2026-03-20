using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Allows static-image enemy scenes (like our duel monsters) to work even if the scene
/// doesn't directly instantiate as NCreatureVisuals, following the EarlyStS2ModdingGuides pattern.
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class StaticImageCreateVisualsPatch
{
    private static readonly MethodInfo _visualsPathGetter = typeof(MonsterModel)
        .GetProperty("VisualsPath", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetGetMethod(true)!;

    public static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        var path = (string)_visualsPathGetter.Invoke(__instance, null)!;
        var scene = PreloadManager.Cache.GetScene(path);

        // C# scenes often root as Node2D + NCreatureVisuals script; Instantiate<T>() throws (see godot.log).
        var root = scene.Instantiate(PackedScene.GenEditState.Disabled);
        if (root is NCreatureVisuals direct)
        {
            __result = direct;
            return false;
        }

        if (root is not Node2D raw)
        {
            root.QueueFree();
            throw new InvalidOperationException(
                $"Creature visuals scene at '{path}' root must be NCreatureVisuals or Node2D, got {root.GetType().Name}.");
        }

        var visuals = new NCreatureVisuals
        {
            Name = raw.Name
        };

        foreach (var child in raw.GetChildren())
        {
            raw.RemoveChild(child);
            visuals.AddChild(child);
            if (child is Node n && n.UniqueNameInOwner)
            {
                n.Owner = visuals;
                n.UniqueNameInOwner = true;
            }
        }

        raw.QueueFree();
        __result = visuals;
        return false;
    }
}
