namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Re-entrant flag while resolving <see cref="YgoDuelist.YgoDuelistCode.Cards.Command.Special_Summon_Wall_Shadow"/> so
/// <see cref="Cards.Monster.Done.Effect.Wall_Shadow.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate"/> is true only for that route
/// (same idea as <see cref="YgoDealWithDarkRulerState"/> for <see cref="Cards.Monster.Done.Effect.Berserk_Dragon"/>).
/// </summary>
public static class YgoMagicalLabyrinthWallShadowSummonState
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
