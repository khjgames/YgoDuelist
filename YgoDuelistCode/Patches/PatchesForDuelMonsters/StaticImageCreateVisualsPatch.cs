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
    private const float StaticPortraitAttributeRaceIconScaleMultiplier = 0.5f;
    private const string StaticPortraitScaledMetaKey = "YgoStaticPortraitIconScaled";

    private static readonly MethodInfo _visualsPathGetter = typeof(MonsterModel)
        .GetProperty("VisualsPath", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetGetMethod(true)!;

    public static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        var path = (string)_visualsPathGetter.Invoke(__instance, null)!;
        var scene = PreloadManager.Cache.GetScene(path);

        // Missing/corrupt exports (e.g. client without duel_monster.tscn in the PCK) leave scene null; Instantiate
        // would NRE, CreatureCmd.Add throws, and MoveCardToMonsterPile never runs → MP state diverges from host.
        Node root;
        if (scene == null)
        {
            GD.PrintErr(
                $"[YgoDuelist][MP] StaticImageCreateVisualsPatch: PackedScene missing for '{path}'. " +
                "Using built-in static portrait layout so summon logic and pile moves stay in sync (reinstall/sync mod).");
            root = CreateFallbackStaticPortraitRoot();
        }
        else
        {
            // C# scenes often root as Node2D + NCreatureVisuals script; Instantiate<T>() throws (see godot.log).
            root = scene.Instantiate(PackedScene.GenEditState.Disabled);
        }
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
        visuals.ChildEnteredTree += OnStaticPortraitChildEnteredTree;
        __result = visuals;
        return false;
    }

    /// <summary>
    /// Mirrors <c>YgoDuelist/monsters/duel_monster/duel_monster.tscn</c> so <see cref="NCreatureVisuals"/> receives the
    /// same %Visuals / %Bounds / marker nodes when the packed scene failed to load (broken install, missing PCK file).
    /// </summary>
    private static Node2D CreateFallbackStaticPortraitRoot()
    {
        var raw = new Node2D { Name = "YgoDuelMonster" };

        var sprite = new Sprite2D
        {
            Name = "Visuals",
            Position = new Vector2(0, -115),
            UniqueNameInOwner = true
        };
        raw.AddChild(sprite);

        var bounds = new Control
        {
            Name = "Bounds",
            UniqueNameInOwner = true,
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = -90f,
            OffsetTop = -110f,
            OffsetRight = 90f,
            OffsetBottom = 0f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        raw.AddChild(bounds);

        var centerPos = new Marker2D
        {
            Name = "CenterPos",
            Position = new Vector2(0, -100),
            UniqueNameInOwner = true
        };
        raw.AddChild(centerPos);

        var intentPos = new Marker2D
        {
            Name = "IntentPos",
            Position = new Vector2(0, -270),
            UniqueNameInOwner = true
        };
        raw.AddChild(intentPos);

        return raw;
    }

    private static void OnStaticPortraitChildEnteredTree(Node child)
    {
        if (!GodotObject.IsInstanceValid(child))
            return;
        ScaleAttributeAndRaceIconsForStaticPortrait(child);
    }

    private static void ScaleAttributeAndRaceIconsForStaticPortrait(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            ScaleAttributeAndRaceIconsForStaticPortrait(child);

            if (child is not CanvasItem canvas)
                continue;

            if (canvas.HasMeta(StaticPortraitScaledMetaKey))
                continue;

            string name = child.Name.ToString().ToLowerInvariant();
            bool isAttributeOrRaceIcon = name.Contains("attribute")
                                         || name.Contains("race")
                                         || name.Contains("typeicon")
                                         || name.Contains("aticon")
                                         || name.Contains("deficon");

            if (!isAttributeOrRaceIcon)
            {
                string texPath = child switch
                {
                    Sprite2D s when s.Texture?.ResourcePath != null => s.Texture.ResourcePath.ToLowerInvariant(),
                    TextureRect t when t.Texture?.ResourcePath != null => t.Texture.ResourcePath.ToLowerInvariant(),
                    _ => string.Empty
                };
                isAttributeOrRaceIcon = texPath.Contains("/attribute/") || texPath.Contains("/race/");
            }
            if (!isAttributeOrRaceIcon)
                continue;

            switch (child)
            {
                case Node2D n2d:
                    n2d.Scale *= Vector2.One * StaticPortraitAttributeRaceIconScaleMultiplier;
                    n2d.SetMeta(StaticPortraitScaledMetaKey, true);
                    break;
                case Control control:
                    control.Scale *= Vector2.One * StaticPortraitAttributeRaceIconScaleMultiplier;
                    control.SetMeta(StaticPortraitScaledMetaKey, true);
                    break;
            }
        }
    }
}
