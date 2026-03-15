namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Final ATK/DEF for a duel monster after applying all field effects.
/// Mirrors the Java DMStats (ATK/DEF) used by DuelMonsterData.calcStats().
/// </summary>
public readonly struct DuelMonsterStats(int atk, int def)
{
    public int Atk { get; } = atk;
    public int Def { get; } = def;
}
