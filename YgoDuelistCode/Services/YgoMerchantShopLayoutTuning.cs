using Godot;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Live-edit fields for YGO merchant buy grid. Vanilla merchant card slots use hover scale 0.8 and idle 0.65
/// on the whole <c>NMerchantCard</c> node (<see cref="MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantSlot"/>);
/// YGO grid slots are patched to use <see cref="MerchantSlotHoverScale"/> / <see cref="MerchantSlotIdleScale"/> instead.
/// </summary>
public static class YgoMerchantShopLayoutTuning
{
    /// <summary>Multiplies default horizontal gap between grid cells (base 18px). 1 = current default.</summary>
    public static float GridHorizontalGapMultiplier = 7.4f;

    /// <summary>Multiplies default vertical gap between grid cells (base 18px). 1 = current default.</summary>
    public static float GridVerticalGapMultiplier = 6.115f;

    /// <summary>
    /// Shifts the YGO buy <c>GridContainer</c> area (inside <c>YgoAddonBuyGridCenter</c>) in pixels. Positive X moves right, positive Y moves down.
    /// </summary>
    public static Vector2 MerchantBuyGridPositionOffset = new Vector2(52f, 65f);

    /// <summary>Matches vanilla <c>NMerchantSlot</c> idle scale after unhover (<c>_smallScale</c>).</summary>
    public static float MerchantSlotIdleScale = 0.56f;

    /// <summary>Matches vanilla <c>NMerchantSlot</c> hover scale on focus (<c>_hoverScale</c>).</summary>
    public static float MerchantSlotHoverScale = 0.8f;

    /// <summary>
    /// After cards bind, one frame at hover scale then idle scale + <c>NCard</c> refresh (mimics first hover cycle for layout).
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
}
