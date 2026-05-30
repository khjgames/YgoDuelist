using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// During targeting, NMouseCardPlay.LerpToMouse sets the holder's target position to the mouse every frame.
/// For option-row plays we clamp that to ~100px from the card's anchor (parent-local) so the card stays put
/// and the arrow is clearly rooted at the NCard.
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "SetTargetPosition")]
public static class NHandCardHolderSetTargetPositionOptionHolderPatch
{
    private const float MaxDragPixels = 100f;

    static void Prefix(NHandCardHolder __instance, ref Vector2 position)
    {
        if (__instance is not NYgoOptionCardHolder opt || !opt.OptionDragAnchor.HasValue)
            return;

        if (__instance.GetParent() is not CanvasItem parent)
            return;
        Viewport? vp = __instance.GetViewport();
        if (vp == null)
            return;

        // position is in viewport space (GetMousePosition). Convert to parent-local, clamp to anchor ± 100, convert back.
        Transform2D parentGlobal = parent.GetGlobalTransform();
        Vector2 globalMouse = vp.GetCanvasTransform().AffineInverse() * position;
        Vector2 parentLocalMouse = parentGlobal.Inverse() * globalMouse;
        Vector2 anchor = opt.OptionDragAnchor.Value;
        Vector2 clampedLocal = new Vector2(
            Mathf.Clamp(parentLocalMouse.X, anchor.X - MaxDragPixels, anchor.X + MaxDragPixels),
            Mathf.Clamp(parentLocalMouse.Y, anchor.Y - MaxDragPixels, anchor.Y + MaxDragPixels));
        Vector2 clampedGlobal = parentGlobal * clampedLocal;
        position = vp.GetCanvasTransform() * clampedGlobal;
    }
}
