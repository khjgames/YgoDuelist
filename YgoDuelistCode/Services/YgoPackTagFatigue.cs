namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Fatigue factor F(d) from post-Tetris accumulation <c>d</c> (see Game_Design/Clarifying_Weighted_Tag_Selection.md).
/// </summary>
public static class YgoPackTagFatigue
{
    public static float FatigueFactor(int d)
    {
        if (d <= 0)
            return 1f;
        if (d == 1)
            return 0.5f;
        float dd = d;
        return 8f / (5f * dd * dd);
    }
}
