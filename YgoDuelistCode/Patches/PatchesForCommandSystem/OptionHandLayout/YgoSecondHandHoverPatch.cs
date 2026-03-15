using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Adds hover-based zooming for NYgoOptionCardHolder so second-hand cards
/// grow to full size on hover (for readability) and shrink back to their
/// compact size when not hovered. Uses a short tween for smooth scale.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "DoCardHoverEffects")]
public static class YgoSecondHandHoverPatch
{
    private const float OptionScaleDuration = 0.04f;
    private static readonly Vector2 OptionSmallScale = Vector2.One * 0.45f;

    static void Postfix(NHandCardHolder __instance, bool isHovered)
    {
        if (__instance is not NYgoOptionCardHolder holder)
            return;

        if (YgoSecondHandHoverState.ScaleTweens.TryGetValue(holder, out var existing) && GodotObject.IsInstanceValid(existing))
        {
            existing.Kill();
            YgoSecondHandHoverState.ScaleTweens.Remove(holder);
        }

        Vector2 targetScale = isHovered ? Vector2.One : OptionSmallScale;
        float targetAngle = isHovered ? 0f : holder.OptionRestAngleDegrees;

        Tween tween = holder.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetParallel(true);
        tween.TweenProperty(holder, "scale", targetScale, OptionScaleDuration);
        tween.TweenProperty(holder, "rotation_degrees", targetAngle, OptionScaleDuration);
        tween.Finished += () => YgoSecondHandHoverState.ScaleTweens.Remove(holder);
        YgoSecondHandHoverState.ScaleTweens[holder] = tween;
    }
}

internal static class YgoSecondHandHoverState
{
    internal static readonly Dictionary<NYgoOptionCardHolder, Tween> ScaleTweens = new();
}
