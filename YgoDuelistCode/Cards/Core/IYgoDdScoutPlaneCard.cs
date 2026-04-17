namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// <see cref="Cards.Monster.Todo.Effect.D_D_Scout_Plane"/>: banish tracking + end-phase return (see <see cref="Services.YgoDdScoutPlaneEndPhase"/>).
/// </summary>
public interface IYgoDdScoutPlaneCard
{
    bool DdScoutEndPhaseUsedThisTurn { get; set; }

    void MarkBanishedThisOwnerTurn(int stamp);

    bool IsBanishedThisTurnForEndPhase(int ownerTurnStamp);
}
