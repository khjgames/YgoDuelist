namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// YGO card-database numbers are often in "hundredths" (e.g. 2000 ATK, 8000 LP). Mod combat values use ÷100 (Cards_Revised.md, Chunk V).
/// Use these helpers when implementing from raw YGO stats instead of hard-coding already-scaled decimals.
/// </summary>
public static class YgoYugiohScale
{
    public static decimal DamageHealBlock(int ygoHundredths) => ygoHundredths / 100m;

    public static decimal DamageHealBlock(decimal ygoHundredths) => ygoHundredths / 100m;
}
