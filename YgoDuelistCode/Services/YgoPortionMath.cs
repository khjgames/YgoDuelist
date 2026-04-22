using System;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Integer split of one damage total into N hits (remainder applied to the last hits).</summary>
public static class YgoPortionMath
{
    /// <summary>
    /// Splits <paramref name="total"/> into <paramref name="portionCount"/> positive integers that sum to <paramref name="total"/>.
    /// Example: 10 / 4 → 2,2,3,3. 10 / 3 → 3,3,4.
    /// </summary>
    public static int[] SplitTotalIntoPortions(int total, int portionCount)
    {
        if (portionCount < 2 || portionCount > 5)
            throw new ArgumentOutOfRangeException(nameof(portionCount), portionCount, "Expected 2–5.");
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        var portions = new int[portionCount];
        if (total == 0)
            return portions;

        int baseEach = total / portionCount;
        int remainder = total % portionCount;
        int firstAugmentedIndex = portionCount - remainder;
        for (int i = 0; i < portionCount; i++)
            portions[i] = baseEach + (i >= firstAugmentedIndex ? 1 : 0);

        return portions;
    }
}
