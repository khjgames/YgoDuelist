namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tunable placement for the “Edit your Deck” corner control (loot-style row).
/// Edit these fields in source to reposition; rest site and shop can use different offsets.
/// </summary>
public static class YgoCampfireDeckEditLayout
{
    // --- Rest site / campfire (NRestSiteRoom) ---

    /// <summary>Pixels from the left edge of the room.</summary>
    public static float RestSiteCornerOffsetX = 24f;

    /// <summary>Pixels up from the bottom edge of the room (larger = higher on screen).</summary>
    public static float RestSiteCornerOffsetY = 140f;

    // --- Merchant / shop (NMerchantRoom), when the buy grid is closed ---

    public static float ShopCornerOffsetX = 24f;

    public static float ShopCornerOffsetY = 140f;

    /// <summary>Added to <see cref="CornerButtonFixedWidthPixels"/> for the row width (loot background + label).</summary>
    public static float CornerButtonHorizontalPaddingPixels = 16f;

    /// <summary>Optional minimum width floor (0 = use fixed width + padding only).</summary>
    public static float CornerButtonMinWidthFloorPixels = 0f;

    /// <summary>Base width before <see cref="CornerButtonHorizontalPaddingPixels"/>.</summary>
    public static float CornerButtonFixedWidthPixels = 385f;

    public static float CornerButtonFixedHeightPixels = 72f;

    /// <summary>Draw order for the full-screen layer that holds the corner button (higher = on top).</summary>
    public static int CornerLayerZIndex = 20;

    /// <summary>Set false to silence GD.Print / PrintErr diagnostics.</summary>
    public static bool DebugLogCornerUi = true;

    // --- Campfire deck edit overlay (NYgoCampfireDeckEditMenuScreen) ---

    /// <summary>
    /// Extra top inset (pixels). Positive values move the entire deck edit GUI downward on screen.
    /// </summary>
    public static float DeckEditMenuContentOffsetY = 120f;

    /// <summary>
    /// Loot stack size: same as <c>Rewards</c> in <c>screens/rewards_screen.tscn</c> (526×640 from center offsets).
    /// </summary>
    public static float DeckEditLootPanelMinWidth = 526f;

    public static float DeckEditLootPanelMinHeight = 640f;

    /// <summary>Verbose layout dump for the deck edit overlay (visibility, modulate, rects).</summary>
    public static bool DebugLogDeckEditMenu = true;
}
