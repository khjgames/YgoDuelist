using System.Threading;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While an MP observer is waiting on <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer.WaitForRemoteChoice"/>
/// for a combat-card grid, records how many <see cref="MegaCrit.Sts2.Core.Entities.Models.PlayerChoiceType.Index"/> entries are valid
/// and how many <b>rows</b> the canonical grid had when this wait began (see <see cref="Active.CandidateRowCount"/>).
/// Used to drop stale pre-buffered Index results whose length does not match the current grid (e.g. 1 index vs discard 2),
/// or whose index values refer to a larger grid than the current one (checksum 86: buffered <c>[1]</c> vs 1-row tribute).
/// </summary>
public static class GridCombatMpExpectation
{
    public readonly struct Active
    {
        public ulong OwnerNetId { get; init; }
        public int MinSelect { get; init; }
        public int MaxSelect { get; init; }

        /// <summary>
        /// Number of selectable rows in the grid snapshot for this wait (0 = skip per-index bounds checks).
        /// </summary>
        public int CandidateRowCount { get; init; }

        /// <summary>
        /// True when this wait accepts combat-card wire results. Indexed-only waits use false so stale card buffers are rejected.
        /// </summary>
        public bool AllowCombatCard { get; init; }
    }

    public static readonly AsyncLocal<Active?> Pending = new();
}
