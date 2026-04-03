using Godot;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Live-edit fields for YGO merchant buy grid. Vanilla shop rows scale the whole <c>NMerchantCard</c>; the YGO grid is
/// inside a <c>GridContainer</c> that resets cell root <see cref="Godot.Control.Scale"/> to (1,1), so idle/hover multipliers
/// are applied to <c>YgoBuyGridScaleRoot</c> (card, hitbox, cost row, sale tag) while the cell root stays at scale 1.
/// </summary>
public static class YgoMerchantShopLayoutTuning
{
    /// <summary>Multiplies default horizontal gap between grid cells (base 18px). 1 = current default.</summary>
    public static float GridHorizontalGapMultiplier = 7.4f;

    /// <summary>Multiplies default vertical gap between grid cells (base 18px). 1 = current default.</summary>
    public static float GridVerticalGapMultiplier = 6.12f;

    /// <summary>
    /// Shifts the YGO buy <c>GridContainer</c> area (inside <c>YgoAddonBuyGridCenter</c>) in pixels. Positive X moves right, positive Y moves down.
    /// </summary>
    public static Vector2 MerchantBuyGridPositionOffset = new Vector2(52f, 70f);

    /// <summary>YGO buy grid: scale on <c>YgoBuyGridScaleRoot</c> when idle (matches vanilla small slot scale intent).</summary>
    public static float MerchantSlotIdleScale = 0.54f;

    /// <summary>YGO buy grid: scale on <c>YgoBuyGridScaleRoot</c> when hovered.</summary>
    public static float MerchantSlotHoverScale = 0.75f;

    /// <summary>
    /// While <c>YgoBuyGridScaleRoot</c> scale is above <see cref="MerchantSlotIdleScale"/>, the <c>Cost</c> row (gold + price)
    /// gets this <see cref="Godot.Control.ZIndex"/> inside the scale root only — not the whole slot (avoids drawing over hover tips).
    /// </summary>
    public static int MerchantBuyGridCostRowZIndexWhenElevated = 24;

    /// <summary>
    /// When opening the YGO buy tab only: one frame at hover scale then idle scale + <c>NCard</c> refresh. Does not run on
    /// inventory re-layout or other deferred refreshes (avoids resetting every slot when one card updates).
    /// </summary>
    public static bool RunOneFrameHoverScalePulseAfterLayout = true;

    /// <summary>
    /// Multiplies <c>_slotsContainer</c> scale when YGO merchant chrome mounts (rug panel + shop rows + mod UI). (1,1) = unchanged.
    /// </summary>
    public static Vector2 MerchantSlotsContainerScaleMultiplier = Vector2.One;

    /// <summary>
    /// Added to <c>_slotsContainer</c> position after scale (pixels).
    /// </summary>
    public static Vector2 MerchantSlotsContainerPositionOffset = Vector2.Zero;

    /// <summary>
    /// Optional: path from <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory"/> root to a rug/carpet-only
    /// <see cref="Control"/> (e.g. <c>%TableRug</c> from Remote scene tree). Empty path skips.
    /// </summary>
    public static NodePath MerchantCarpetOnlyNodePath = new NodePath();

    /// <summary>
    /// Multiplies the carpet-only node's scale at mount (after slots-container tuning). (1,1) = unchanged.
    /// </summary>
    public static Vector2 MerchantCarpetOnlyScaleMultiplier = Vector2.Zero;

    /// <summary>
    /// Added to the carpet-only node's position (pixels).
    /// </summary>
    public static Vector2 MerchantCarpetOnlyPositionOffset = Vector2.Zero;

    // --- Room-world Card Trader NPC (merchant room, next to vanilla merchant) ---

    /// <summary>
    /// Scale applied to the Card Trader <see cref="TextureButton"/> after matching the merchant hit rect <see cref="Control.Size"/>.
    /// (1,1) = same on-screen size as the merchant control; default <c>0.4</c> is 40% of that footprint.
    /// </summary>
    public static Vector2 CardTraderRoomNpcScaleMultiplier = new Vector2(0.4f, 0.4f);

    /// <summary>
    /// Added to the Card Trader's global position after gap alignment (pixels). Positive X moves right, positive Y moves down.
    /// </summary>
    public static Vector2 CardTraderRoomNpcPositionOffset = Vector2.Zero;

    /// <summary>
    /// Pixels between the Card Trader's scaled right edge and the vanilla merchant's left edge.
    /// </summary>
    public static float CardTraderRoomNpcGapPixels = 50f;

    // --- YGO buy grid: bundled-card stack (see YgoDuelistCard.BundledCards) ---

    /// <summary>
    /// Fallback uniform scale for bundle previews when the main offer <c>NCard</c> is missing (normally previews copy the main card's <c>Scale</c>).
    /// </summary>
    public static float MerchantBundlePreviewScaleFallback = 1f;

    /// <summary>Offset per stacked card: positive X = right, negative Y = up (Godot Y-down).</summary>
    public static Vector2 MerchantBundlePreviewStepPixels = new(36f, -28f);

    /// <summary>
    /// Extra offset applied after the bundle anchor is computed from the holder bottom (see <see cref="YgoMerchantShopBundleVisual"/>).
    /// </summary>
    public static Vector2 MerchantBundlePreviewOriginPixels = Vector2.Zero;

    /// <summary>Pixels subtracted from holder height when anchoring the first bundle preview (larger = higher on screen).</summary>
    public static float MerchantBundlePreviewAnchorFromBottomPx = 88f;

    /// <summary>
    /// Added to every bundle preview <see cref="Godot.Control.Position"/> after base anchor and fan <see cref="MerchantBundlePreviewStepPixels"/> (pixels in holder space).
    /// Positive X = right; negative Y = up on screen (Godot Y-down).
    /// </summary>
    public static Vector2 MerchantBundlePreviewPositionOffsetPixels = new(25f, -130f);
}
