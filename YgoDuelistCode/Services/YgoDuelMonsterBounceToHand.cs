using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// When a duel monster pet is killed, <see cref="DuelMonsterPetDeathPatch"/> checks this set so the source card
/// moves to hand instead of the graveyard (equips still go to the graveyard).
/// </summary>
public static class YgoDuelMonsterBounceToHand
{
    private static readonly HashSet<Creature> Pending = new();

    public static void RegisterForHandReturn(Creature pet)
    {
        if (pet != null)
            Pending.Add(pet);
    }

    public static bool TryConsume(Creature pet) =>
        pet != null && Pending.Remove(pet);
}
