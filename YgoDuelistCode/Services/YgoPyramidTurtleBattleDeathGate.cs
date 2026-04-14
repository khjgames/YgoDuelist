using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Cards.Monster.Todo.Effect.Pyramid_Turtle"/>: search only when destroyed by battle;
/// <see cref="Patches.DuelMonsterPetDeathPatch"/> marks the card before it hits the Graveyard.
/// </summary>
public static class YgoPyramidTurtleBattleDeathGate
{
    private static readonly HashSet<CardModel> Marked = new();

    public static void Mark(CardModel card) => Marked.Add(card);

    public static bool Consume(CardModel card) => Marked.Remove(card);

    public static void ClearAll() => Marked.Clear();
}
