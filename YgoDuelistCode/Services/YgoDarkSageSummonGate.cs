namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Re-entrant flag while resolving <see cref="YgoDuelist.YgoDuelistCode.Cards.Command.Special_Summon_Dark_Sage"/> so
/// <see cref="Cards.Monster.Done.Effect.Dark_Sage.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate"/> is true only for that route.
/// </summary>
public static class YgoDarkSageSummonGate
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
