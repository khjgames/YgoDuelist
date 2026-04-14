using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Single pending mark: pet died by battle and the source card is about to hit the Graveyard — consumed by the matching GY handler.
/// </summary>
public static class YgoBattleDeathMarkedCards
{
    private static readonly HashSet<CardModel> Marked = new();

    public static void Mark(CardModel card) => Marked.Add(card);

    public static bool Consume(CardModel card) => Marked.Remove(card);

    public static void ClearAll() => Marked.Clear();
}
