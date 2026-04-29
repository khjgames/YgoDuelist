namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Marker for cards that use the <c>Brick</c> keyword: they belong in the discard pile for combat flow and are
/// stripped out of the draw pile when it is built or shuffled (<see cref="YgoBrickCardBootstrap"/>).
/// </summary>
public interface IYgoBrickCard
{
}
