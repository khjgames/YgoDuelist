using System.Collections.Generic;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Authoritative enqueue-time card resource spend snapshot from trailing NetPlayCardAction bits.
/// Consumed once by observer-side PlayCardAction mirror to avoid local cost recompute drift.
/// </summary>
public static class YgoNetPlayCardResourceSpendStash
{
    private static readonly Dictionary<uint, (int Energy, int Stars)> Pending = new();
    private static readonly object Gate = new();

    public static void Store(uint combatCardIndex, int energy, int stars)
    {
        lock (Gate)
            Pending[combatCardIndex] = (energy, stars);
    }

    public static bool TryTake(uint combatCardIndex, out int energy, out int stars)
    {
        lock (Gate)
        {
            if (Pending.TryGetValue(combatCardIndex, out var t))
            {
                Pending.Remove(combatCardIndex);
                energy = t.Energy;
                stars = t.Stars;
                return true;
            }
        }

        energy = 0;
        stars = 0;
        return false;
    }
}
