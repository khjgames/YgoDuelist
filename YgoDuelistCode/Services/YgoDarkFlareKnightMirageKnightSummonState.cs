namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Re-entrant flag while resolving Special Summon of Mirage Knight from Dark Flare Knight battle destruction
/// (<see cref="YgoGraveyardOptionalDeckSpecialSummon"/>).
/// </summary>
public static class YgoDarkFlareKnightMirageKnightSummonState
{
    private static int _summonBypassDepth;

    public static bool IsSummonBypassActive => _summonBypassDepth > 0;

    public static void EnterSummonBypass() => _summonBypassDepth++;

    public static void ExitSummonBypass()
    {
        if (_summonBypassDepth > 0)
            _summonBypassDepth--;
    }
}
