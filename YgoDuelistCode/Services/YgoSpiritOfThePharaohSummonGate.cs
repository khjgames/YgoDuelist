namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Re-entrant gate for <see cref="Cards.Monster.Done.Effect.Spirit_of_the_Pharaoh"/> Special Summon via the Sarcophagus trio.
/// </summary>
public static class YgoSpiritOfThePharaohSummonGate
{
    private static int _depth;

    public static bool IsActive => _depth > 0;

    public static void Enter() => _depth++;

    public static void Exit()
    {
        if (_depth > 0)
            _depth--;
    }
}
