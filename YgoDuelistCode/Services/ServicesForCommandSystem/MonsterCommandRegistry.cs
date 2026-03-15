using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YgoDuelist.YgoDuelistCode.Services;

public sealed class MonsterCommandState
{
    public bool DieForYouEnabled;
    public bool HasUsedCommandThisTurn;
}

public static class MonsterCommandRegistry
{
    private static readonly Dictionary<Creature, MonsterCommandState> _states = new();

    public static MonsterCommandState GetOrCreate(Creature pet)
    {
        if (!_states.TryGetValue(pet, out var state))
        {
            state = new MonsterCommandState();
            _states[pet] = state;
        }
        return state;
    }

    public static bool TryGet(Creature pet, out MonsterCommandState state)
        => _states.TryGetValue(pet, out state!);

    public static void Clear(Creature pet)
    {
        _states.Remove(pet);
    }

    /// <summary>Clears all command state. Call at end of combat.</summary>
    public static void ClearAll()
    {
        _states.Clear();
    }
}
