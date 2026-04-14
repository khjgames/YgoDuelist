namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Field monster that tracks local spell counters.
/// </summary>
public interface IYgoSpellCounterMonster
{
    int CurrentSpellCounters { get; }
    int MaxSpellCounters { get; }
    void AddSpellCounter(int amount = 1);
    bool TryConsumeSpellCounters(int amount);
}
