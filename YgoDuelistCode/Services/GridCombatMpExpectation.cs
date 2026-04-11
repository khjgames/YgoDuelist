using System.Threading;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While an MP observer is waiting on <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer.WaitForRemoteChoice"/>
/// for a combat-card grid, records how many <see cref="MegaCrit.Sts2.Core.Entities.Models.PlayerChoiceType.Index"/> entries are valid.
/// Used to drop stale pre-buffered Index results whose length does not match the current grid (e.g. 1 index vs discard 2).
/// </summary>
public static class GridCombatMpExpectation
{
    public readonly struct Active
    {
        public ulong OwnerNetId { get; init; }
        public int MinSelect { get; init; }
        public int MaxSelect { get; init; }
    }

    public static readonly AsyncLocal<Active?> Pending = new();
}
