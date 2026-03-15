namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Aggregate ATK/DEF changes applied by support monsters (Star Boy, Bladefly, etc.).
/// Mirrors the Java mod's StatEffectTotal but currently only uses flat bonuses.
/// </summary>
public readonly struct StatEffectTotal(int bonusAtk, int bonusDef)
{
    public int BonusAtk { get; } = bonusAtk;
    public int BonusDef { get; } = bonusDef;

    public static readonly StatEffectTotal None = new StatEffectTotal(0, 0);
}

