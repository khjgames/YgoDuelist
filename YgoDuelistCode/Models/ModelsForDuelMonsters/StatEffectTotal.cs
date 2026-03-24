namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Aggregate ATK/DEF/level changes from support monsters (Star Boy, etc.) and face-up field spells.
/// </summary>
public readonly struct StatEffectTotal(int bonusAtk, int bonusDef, int bonusLevel = 0)
{
    public int BonusAtk { get; } = bonusAtk;
    public int BonusDef { get; } = bonusDef;
    /// <summary>Effective level adjustment from field spells (e.g. A Legendary Ocean: -1 for WATER).</summary>
    public int BonusLevel { get; } = bonusLevel;

    public static readonly StatEffectTotal None = new StatEffectTotal(0, 0, 0);
}

