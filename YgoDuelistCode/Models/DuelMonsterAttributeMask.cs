namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>Bitmask for fusion (and similar) filters: one bit per <see cref="DuelMonsterAttribute"/> value.</summary>
[Flags]
public enum DuelMonsterAttributeMask
{
    None = 0,
    Earth = 1 << 0,
    Water = 1 << 1,
    Fire = 1 << 2,
    Wind = 1 << 3,
    Light = 1 << 4,
    Dark = 1 << 5
}

public static class DuelMonsterAttributeMaskExtensions
{
    public static DuelMonsterAttributeMask ToMask(this DuelMonsterAttribute attribute) =>
        (DuelMonsterAttributeMask)(1 << (int)attribute);

    public static DuelMonsterAttributeMask Combine(params DuelMonsterAttribute[] attributes)
    {
        DuelMonsterAttributeMask m = DuelMonsterAttributeMask.None;
        foreach (DuelMonsterAttribute a in attributes)
            m |= a.ToMask();
        return m;
    }
}
