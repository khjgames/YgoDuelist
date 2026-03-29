namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Multiplies summed ATK/DEF after flat <see cref="StatEffectTotal"/> bonuses (e.g. Limiter Removal on Machine monsters).
/// </summary>
public readonly struct StatEffectTotalMultiplier(decimal atk, decimal def)
{
    public decimal Atk { get; } = atk;
    public decimal Def { get; } = def;

    public static StatEffectTotalMultiplier Identity => new(1m, 1m);

    public static StatEffectTotalMultiplier LimiterRemovalMachine => new(2m, 1m);

    public static StatEffectTotalMultiplier MegamorphDoubleAtk => new(2m, 1m);

    public static StatEffectTotalMultiplier MegamorphHalveAtk => new(0.5m, 1m);

    /// <summary>Hourglass of Courage: normal/tribute summon debuff (halve ATK/DEF) until timer expires.</summary>
    public static StatEffectTotalMultiplier HourglassOfCourageNormalSummon => new(0.5m, 0.5m);
}
