using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO buy grid: <c>GridContainer</c> keeps <c>NMerchantCard</c> root at scale (1,1). Vanilla merchant scales the whole
/// slot so <c>%CardHolder</c>, <c>%Hitbox</c>, <c>Cost</c> (gold + <c>%CostLabel</c>), and <c>%SaleVisual</c> move together.
/// We insert <see cref="ScaleRootNodeName"/> under the cell, reparent those nodes into it (preserve global transform),
/// and only scale that container — same behavior as normal shop hover/idle.
/// </summary>
internal static class YgoBuyGridMerchantSlotChromeScale
{
    internal const string ScaleRootNodeName = "YgoBuyGridScaleRoot";

    internal static Control? GetScaleRoot(NMerchantCard slot) =>
        slot.GetNodeOrNull<Control>(ScaleRootNodeName);

    /// <summary>
    /// One-time: wrap vanilla merchant visuals so a single scale drives card, hitbox, cost row, and sale tag.
    /// </summary>
    internal static void EnsureBuyGridScaledContentRoot(NMerchantCard slot)
    {
        if (GetScaleRoot(slot) != null)
            return;

        Node? cardHolder = slot.GetNodeOrNull("%CardHolder");
        if (cardHolder == null || cardHolder.GetParent() != slot)
            return;

        var root = new Control
        {
            Name = ScaleRootNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.OffsetLeft = 0;
        root.OffsetTop = 0;
        root.OffsetRight = 0;
        root.OffsetBottom = 0;

        slot.AddChild(root);
        slot.MoveChild(root, 0);

        ReparentIfDirectChild(slot, root, cardHolder);
        ReparentIfDirectChild(slot, root, slot.GetNodeOrNull("%Hitbox"));
        ReparentIfDirectChild(slot, root, slot.GetNodeOrNull("Cost"));
        ReparentIfDirectChild(slot, root, slot.GetNodeOrNull("%SaleVisual"));
    }

    private static void ReparentIfDirectChild(Node slot, Control root, Node? node)
    {
        if (node != null && node.GetParent() == slot)
            node.Reparent(root, keepGlobalTransform: true);
    }

    internal static void ApplyChromeUniformScale(NMerchantCard slot, float uniformScale)
    {
        EnsureBuyGridScaledContentRoot(slot);
        slot.Scale = Vector2.One;
        Control? root = GetScaleRoot(slot);
        if (root != null)
            root.Scale = Vector2.One * uniformScale;
        SyncSortOrderForChromeScale(slot, uniformScale);
    }

    internal static void ApplyChromeHoverInstant(NMerchantCard slot) =>
        ApplyChromeUniformScale(slot, YgoMerchantShopLayoutTuning.MerchantSlotHoverScale);

    /// <summary>Requires <c>EnsureBuyGridScaledContentRoot</c> so the scale root exists.</summary>
    internal static void TweenChromeToIdle(NMerchantCard slot, Tween tween, float durationSeconds)
    {
        Control? root = GetScaleRoot(slot);
        if (root == null)
            return;

        Vector2 idle = Vector2.One * YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
        tween.TweenProperty(root, "scale", idle, durationSeconds)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        float idleScalar = YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
        tween.TweenCallback(Callable.From(() => SyncSortOrderForChromeScale(slot, idleScalar)));
    }

    /// <summary>
    /// While scaled above idle, raise only the <c>Cost</c> row (gold + price) inside the scale root so it paints above
    /// card art / sale tag in the same cell — not <c>NMerchantCard</c> itself (that would stack the whole slot over hover tips).
    /// </summary>
    internal static void SyncSortOrderForChromeScale(NMerchantCard slot, float chromeUniformScale)
    {
        float idle = YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
        bool elevated = chromeUniformScale > idle + 0.0001f;

        Control? costRow = GetScaleRoot(slot)?.GetNodeOrNull<Control>("Cost");
        if (costRow != null)
            costRow.ZIndex = elevated ? YgoMerchantShopLayoutTuning.MerchantBuyGridCostRowZIndexWhenElevated : 0;
    }
}
