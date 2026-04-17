namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Field monster: at start of your turn, enemies whose attack intent vs you is at least this threshold get 1 Weak
/// (used by `IYgoOwnerTurnStartFieldMonsterEffect` implementations such as `Gora_Turtle`).
/// </summary>
public interface IYgoTurnStartWeakFromAttackIntent
{
    int AttackIntentWeakThreshold { get; }
}
