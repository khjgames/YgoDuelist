using System;
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
    private sealed class Scope : IDisposable
    {
        private readonly Active? _previous;
        private bool _disposed;

        public Scope(Active? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Pending.Value = _previous;
        }
    }

    public readonly struct Active
    {
        public ulong OwnerNetId { get; init; }
        public uint? ExpectedChoiceId { get; init; }
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

        /// <summary>
        /// True when this wait accepts index wire results. Vanilla hand-card selectors use combat-card wire only.
        /// </summary>
        public bool AllowIndex { get; init; }

        /// <summary>
        /// True when this wait accepts deck-card wire results.
        /// </summary>
        public bool AllowDeckCard { get; init; }

        /// <summary>
        /// True when this wait accepts canonical-card wire results.
        /// </summary>
        public bool AllowCanonicalCard { get; init; }

        /// <summary>
        /// True when this wait accepts mutable-card wire results.
        /// </summary>
        public bool AllowMutableCard { get; init; }

        /// <summary>
        /// True when this wait accepts player wire results.
        /// </summary>
        public bool AllowPlayer { get; init; }

        /// <summary>
        /// True when an index result may contain -1 (vanilla skip/cancel sentinel).
        /// </summary>
        public bool AllowNegativeIndex { get; init; }
    }

    public static readonly AsyncLocal<Active?> Pending = new();

    public static IDisposable Push(Active active)
    {
        Active? previous = Pending.Value;
        Pending.Value = active;
        return new Scope(previous);
    }

    public static Active WithExpectedChoiceId(Active active, uint choiceId) =>
        new()
        {
            OwnerNetId = active.OwnerNetId,
            ExpectedChoiceId = choiceId,
            MinSelect = active.MinSelect,
            MaxSelect = active.MaxSelect,
            CandidateRowCount = active.CandidateRowCount,
            AllowCombatCard = active.AllowCombatCard,
            AllowIndex = active.AllowIndex,
            AllowDeckCard = active.AllowDeckCard,
            AllowCanonicalCard = active.AllowCanonicalCard,
            AllowMutableCard = active.AllowMutableCard,
            AllowPlayer = active.AllowPlayer,
            AllowNegativeIndex = active.AllowNegativeIndex
        };
}
