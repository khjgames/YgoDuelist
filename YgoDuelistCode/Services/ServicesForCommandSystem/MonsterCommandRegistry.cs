using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Powers;

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

    public static async Task SetHasUsedCommandThisTurn(Creature pet, bool hasUsedCommandThisTurn, Creature? applier = null, CardModel? sourceCard = null)
    {
        var state = GetOrCreate(pet);
        state.HasUsedCommandThisTurn = hasUsedCommandThisTurn;

        if (hasUsedCommandThisTurn)
        {
            await PowerCmd.Apply<StiffPower>(pet, 1m, applier, sourceCard);
            await PowerCmd.Apply<FatiguePower>(pet, 1m, applier, sourceCard);
            return;
        }

        await PowerCmd.Remove<StiffPower>(pet);
        await PowerCmd.Remove<FatiguePower>(pet);
    }

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
