using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public sealed class MonsterCommandState
{
    public bool DieForYouEnabled;
    public bool DieForYouForced;
    public bool HasUsedCommandThisTurn;

    /// <summary>Once per turn for <see cref="Command.Activate_Effect"/> only; does not apply stiff/fatigue.</summary>
    public bool HasUsedActivatedEffectThisTurn;

    /// <summary>Cyber Jar: Command Attack/Defend cost 0 for this pet until end of turn.</summary>
    public bool ZeroEnergyMonsterCommandsThisTurn;

    /// <summary>D.D. Warrior Lady: Activate Effect usable after this pet resolved an attack this turn.</summary>
    public bool WarriorLadyBanishWindowActive;
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

    public static void SetHasUsedActivatedEffectThisTurn(Creature pet, bool used)
    {
        GetOrCreate(pet).HasUsedActivatedEffectThisTurn = used;
    }

    public static async Task SetHasUsedCommandThisTurn(Creature pet, bool hasUsedCommandThisTurn, Creature? applier = null, CardModel? sourceCard = null)
    {
        var state = GetOrCreate(pet);
        state.HasUsedCommandThisTurn = hasUsedCommandThisTurn;

        if (!hasUsedCommandThisTurn)
            state.HasUsedActivatedEffectThisTurn = false;

        if (hasUsedCommandThisTurn)
        {
            await PowerCmd.Apply<StiffPower>(pet, 1m, applier, sourceCard);
            await PowerCmd.Apply<FatiguePower>(pet, 1m, applier, sourceCard);
            return;
        }

        await PowerCmd.Remove<StiffPower>(pet);
        await PowerCmd.Remove<FatiguePower>(pet);
    }

    /// <summary>
    /// <see cref="StiffPower"/> only: does not apply <see cref="FatiguePower"/> and does not change
    /// <see cref="MonsterCommandState.HasUsedCommandThisTurn"/>. Used for the battle-position command menu action.
    /// </summary>
    public static async Task ApplyStiffFromBattlePositionChangeOnly(Creature pet, Creature? applier = null, CardModel? sourceCard = null)
    {
        await PowerCmd.Apply<StiffPower>(pet, 1m, applier, sourceCard);
    }

    /// <summary>
    /// Forced Die For You (Cards_Revised Chunk Y): menu toggle is hidden in <see cref="DuelMonsterMonsterOptionsMenu"/>,
    /// and the source card's attack/defense/hand-effect right-click toggle is blocked while the summon is on the field.
    /// </summary>
    public static bool SourceMonsterHasDieForYouForcedActive(Player? player, BaseMonsterCard? sourceCard)
    {
        if (player?.PlayerCombatState == null || sourceCard == null)
            return false;

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) != sourceCard)
                continue;
            return TryGet(pet, out var s) && s.DieForYouForced;
        }

        return false;
    }

    /// <summary>When forced, Die For You stays on and the toggle command is hidden.</summary>
    public static async Task SetDieForYouForcedAsync(Creature pet, bool forced, Player player, NormalMonsterCard? sourceCard)
    {
        var state = GetOrCreate(pet);
        state.DieForYouForced = forced;
        if (forced)
        {
            state.DieForYouEnabled = true;
            await PowerCmd.Apply<DieForYouPower>(pet, 1m, player.Creature, sourceCard);
        }
        else
        {
            state.DieForYouEnabled = false;
            await PowerCmd.Remove<DieForYouPower>(pet);
        }
    }

    public static void Clear(Creature pet)
    {
        _states.Remove(pet);
    }

    /// <summary>End of player turn: Cyber Jar free commands and D.D. Warrior Lady attack-gated window.</summary>
    public static void ClearPerTurnExtrasForPlayer(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!TryGet(pet, out MonsterCommandState s))
                continue;
            s.ZeroEnergyMonsterCommandsThisTurn = false;
            s.WarriorLadyBanishWindowActive = false;
        }
    }

    /// <summary>Clears all command state. Call at end of combat.</summary>
    public static void ClearAll()
    {
        _states.Clear();
    }
}
