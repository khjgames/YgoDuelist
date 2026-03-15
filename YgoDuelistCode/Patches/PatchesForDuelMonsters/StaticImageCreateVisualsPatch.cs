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

        try
        {
            __result = scene.Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
            return false;
        }
        catch (InvalidCastException)
        {
            // Mod scene didn't register the script properly, fall through to manual wrap.
        }

        var raw = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
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
