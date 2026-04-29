namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While &gt; 0, <see cref="Cards.Spell.Done.Continuos.The_Second_Sarcophagus"/> / <see cref="Cards.Spell.Done.Continuos.The_Third_Sarcophagus"/>
/// may be played from the hand (only via <see cref="YgoSarcophagusChain"/>).
/// </summary>
public static class YgoFirstSarcophagusPlacementGate
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
