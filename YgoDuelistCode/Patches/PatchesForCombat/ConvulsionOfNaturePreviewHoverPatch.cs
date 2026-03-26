using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Hover-based zooming for the Convulsion Of Nature preview card, matching the
/// same tween-based behavior used by the second-hand option row.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "DoCardHoverEffects")]
public static class ConvulsionOfNaturePreviewHoverPatch
{
    private const float ScaleDuration = 0.04f;
    private static readonly Vector2 PreviewSmallScale = Vector2.One * 0.36f;

    static void Postfix(NHandCardHolder __instance, bool isHovered)
    {
        if (__instance is not NYgoConvulsionPreviewHolder holder)
            return;

        if (ConvulsionOfNaturePreviewHoverState.ScaleTweens.TryGetValue(holder, out var existing) && GodotObject.IsInstanceValid(existing))
        {
            existing.Kill();
            ConvulsionOfNaturePreviewHoverState.ScaleTweens.Remove(holder);
        }

        Vector2 targetScale = isHovered ? Vector2.One : PreviewSmallScale;

        Tween tween = holder.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(holder, "scale", targetScale, ScaleDuration);
        tween.Finished += () => ConvulsionOfNaturePreviewHoverState.ScaleTweens.Remove(holder);
        ConvulsionOfNaturePreviewHoverState.ScaleTweens[holder] = tween;
    }
}

internal static class ConvulsionOfNaturePreviewHoverState
{
    internal static readonly Dictionary<NYgoConvulsionPreviewHolder, Tween> ScaleTweens = new();
}

