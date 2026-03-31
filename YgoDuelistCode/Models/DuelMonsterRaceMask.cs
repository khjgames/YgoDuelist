namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>Bitmask for fusion filters over <see cref="DuelMonsterRace"/> (monster races only; spell/trap races can be included if needed).</summary>
public readonly struct DuelMonsterRaceMask
{
    public ulong Bits { get; }

    public DuelMonsterRaceMask(ulong bits) => Bits = bits;

    public static DuelMonsterRaceMask Of(DuelMonsterRace race) => new((ulong)1u << (int)race);

    public static DuelMonsterRaceMask Combine(params DuelMonsterRace[] races)
    {
        ulong u = 0;
        foreach (DuelMonsterRace r in races)
            u |= (ulong)1u << (int)r;
        return new DuelMonsterRaceMask(u);
    }

    public static DuelMonsterRaceMask operator |(DuelMonsterRaceMask a, DuelMonsterRaceMask b) =>
        new(a.Bits | b.Bits);

    public bool Contains(DuelMonsterRace race) => (Bits & ((ulong)1u << (int)race)) != 0;
}
