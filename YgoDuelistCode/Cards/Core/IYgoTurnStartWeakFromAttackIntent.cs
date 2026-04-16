namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Field monster: at start of your turn, enemies whose attack intent vs you is at least this threshold get 1 Weak
/// (<see cref="YgoDuelist.YgoDuelistCode.Services.YgoGoraTurtleService"/>).
/// </summary>
public interface IYgoTurnStartWeakFromAttackIntent
{
    int AttackIntentWeakThreshold { get; }
}
