namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Live-edit layout for compendium card library (filter / sort sidebar).</summary>
public static class YgoCardLibraryLayoutTuning
{
    /// <summary>
    /// Empty margin reserved below the filter/sort <see cref="Godot.ScrollContainer"/> (pixels).
    /// The scroll area height is the sidebar slice minus this value so bottom UI stays visible.
    /// </summary>
    public static float FilterScrollBottomReservePixels = 155f;
}
