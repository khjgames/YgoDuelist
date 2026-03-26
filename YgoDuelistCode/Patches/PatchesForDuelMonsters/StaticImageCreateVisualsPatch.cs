using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Allows static-image enemy scenes (like duel_monster.tscn) to work when the packed root is
/// plain Node2D (no C# script on the scene: exported PCKs do not ship YgoDuelistCode .cs sources).
/// Reparents children onto <c>new NCreatureVisuals()</c> from the game assembly.
/// Registered explicitly from mod <c>Initialize</c> so it always applies.
/// </summary>
public static class StaticImageCreateVisualsPatch
{
    private const float StaticPortraitAttributeRaceIconScaleMultiplier = 2.0f;

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
        ScaleAttributeAndRaceIconsForStaticPortrait(visuals);
        __result = visuals;
        return false;
    }

    private static void ScaleAttributeAndRaceIconsForStaticPortrait(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            ScaleAttributeAndRaceIconsForStaticPortrait(child);

            string name = child.Name.ToString().ToLowerInvariant();
            bool isAttributeOrRaceIcon =
                name.Contains("attribute") || name.Contains("race");
            if (!isAttributeOrRaceIcon)
                continue;

            switch (child)
            {
                case Node2D n2d:
                    n2d.Scale *= Vector2.One * StaticPortraitAttributeRaceIconScaleMultiplier;
                    break;
                case Control control:
                    control.Scale *= Vector2.One * StaticPortraitAttributeRaceIconScaleMultiplier;
                    break;
            }
        }
    }
}
