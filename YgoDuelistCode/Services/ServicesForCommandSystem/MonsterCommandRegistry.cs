using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
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

    /// <summary>D.D. Warrior / D.D. Warrior Lady: set true after this pet&apos;s attack damage resolves (hand or command attack); cleared at your turn start.</summary>
    public bool HasAttackedThisTurn;

    /// <summary>Exarion Universe: activated effect — Splinter on attacks and -4 ATK until end of turn.</summary>
    public bool ExarionUniversePiercingStanceThisTurn;

    /// <summary>Karate Man activated effect: bonus ATK from Mgc until end of turn.</summary>
    public bool KarateManBurstAtkThisTurn;

    /// <summary>Karate Man: destroy this pet at end of the controlling player turn after burst.</summary>
    public bool KarateManDestroyAtEndOfOwnerTurn;

    /// <summary>Guardian Slime activated effect: destroy at end of the controlling player turn.</summary>
    public bool GuardianSlimeDestroyAtEndOfOwnerTurn;

    /// <summary>Second Activate Effect option (e.g. Reactor Slime Metal Reflect).</summary>
    public bool HasUsedSecondActivatedEffectThisTurn;

    /// <summary>Per-turn Command Attack slot when <see cref="BaseMonsterCard.AllowsSeparateAttackAndDefendCommandsPerTurn"/>.</summary>
    public bool HasUsedAttackCommandThisTurn;

    /// <summary>Per-turn Command Defend slot when <see cref="BaseMonsterCard.AllowsSeparateAttackAndDefendCommandsPerTurn"/>.</summary>
    public bool HasUsedDefendCommandThisTurn;

    /// <summary><see cref="Cards.Monster.Todo.Effect.Gray_Wing"/>: activated — −Mgc ATK and +1 attack resolution this turn.</summary>
    public int GrayWingAtkPenaltyThisTurn;

    public bool GrayWingDoubleAttackThisTurn;

    /// <summary><see cref="Cards.Monster.Todo.Effect.Tyrant_Dragon"/>: activated — +1 attack resolution this turn.</summary>
    public bool TyrantDragonDoubleAttackThisTurn;

    /// <summary><see cref="Cards.Monster.Todo.Effect.Catapult_Turtle"/>: once per turn activated tribute Blight.</summary>
    public bool CatapultTurtleActivatedThisTurn;

    /// <summary><see cref="Cards.Monster.Todo.Effect.Goddess_of_Whim"/>: 1.0 = none, 2.0 = double, 0.5 = halve printed ATK this turn.</summary>
    public decimal GoddessOfWhimAtkMultiplierThisTurn = 1m;

    /// <summary>Set when this pet was killed by enemy combat damage (Move); read before <see cref="MonsterCommandRegistry.Clear"/>.</summary>
    public bool DestroyedByEnemyBattleDamage;

    /// <summary>Enemy creature that dealt the killing Move blow; set with <see cref="DestroyedByEnemyBattleDamage"/>.</summary>
    public Creature? BattleDamageKillerEnemy;

    /// <summary>When true, turn-start lock clear is skipped once (used by cards whose post-attack lock persists).</summary>
    public bool KeepCommandLockOnNextTurnStart;

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

    public static void SetHasUsedSecondActivatedEffectThisTurn(Creature pet, bool used)
    {
        GetOrCreate(pet).HasUsedSecondActivatedEffectThisTurn = used;
    }

    public static async Task SetHasUsedCommandThisTurn(Creature pet, bool hasUsedCommandThisTurn, Creature? applier = null, CardModel? sourceCard = null)
    {
        var state = GetOrCreate(pet);
        state.HasUsedCommandThisTurn = hasUsedCommandThisTurn;

        if (!hasUsedCommandThisTurn)
        {
            state.HasUsedActivatedEffectThisTurn = false;
            state.HasUsedSecondActivatedEffectThisTurn = false;
            state.HasUsedAttackCommandThisTurn = false;
            state.HasUsedDefendCommandThisTurn = false;
        }

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
    /// MP checksum only: same registry fields and Stiff/Fatigue removal as <see cref="SetHasUsedCommandThisTurn"/> (false),
    /// but uses <see cref="PowerModel.RemoveInternal"/> only. Do not use <see cref="PowerCmd.Remove{T}"/> with
    /// <c>GetResult()</c> on the main thread — it awaits animation delays and can freeze the process.
    /// </summary>
    public static void ResetCommandLockStateSyncForChecksum(Creature pet)
    {
        var state = GetOrCreate(pet);
        state.HasUsedCommandThisTurn = false;
        state.HasUsedActivatedEffectThisTurn = false;
        state.HasUsedSecondActivatedEffectThisTurn = false;
        state.HasUsedAttackCommandThisTurn = false;
        state.HasUsedDefendCommandThisTurn = false;

        RemovePowerIfPresentSyncForChecksum<StiffPower>(pet);
        RemovePowerIfPresentSyncForChecksum<FatiguePower>(pet);
    }

    private static void RemovePowerIfPresentSyncForChecksum<T>(Creature pet) where T : PowerModel
    {
        T? power = pet.GetPower<T>();
        if (power != null)
            power.RemoveInternal();
    }

    /// <summary>
    /// MP checksum / <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetFullCombatState.FromRun"/> only:
    /// align <see cref="DieForYouPower"/> with registry + field card without awaiting <see cref="PowerCmd.Apply"/>,
    /// which can deadlock when blocked with <c>GetResult()</c> on the main thread.
    /// </summary>
    public static void ApplyDieForYouSyncForChecksum(Creature pet, Creature applier, CardModel? sourceCard)
    {
        if (CombatManager.Instance?.IsEnding == true)
            return;
        if (pet.HasPower<DieForYouPower>())
            return;

        PowerModel proto = ModelDb.Power<DieForYouPower>();
        PowerModel power = proto.ToMutable();
        power.Applier = applier;
        power.ApplyInternal(pet, 1m, silent: true);
    }

    public static bool CanUseMonsterAttackCommand(Creature pet, NormalMonsterCard? sourceMonster)
    {
        var state = GetOrCreate(pet);
        if (sourceMonster is BaseMonsterCard bm && bm.AllowsSeparateAttackAndDefendCommandsPerTurn)
        {
            // Summon sickness / other full lockout: HasUsedCommandThisTurn with no slot spent yet.
            return !state.HasUsedAttackCommandThisTurn
                   && (!state.HasUsedCommandThisTurn || state.HasUsedDefendCommandThisTurn);
        }

        return !state.HasUsedCommandThisTurn;
    }

    public static bool CanUseMonsterDefendCommand(Creature pet, NormalMonsterCard? sourceMonster)
    {
        var state = GetOrCreate(pet);
        if (sourceMonster is BaseMonsterCard bm && bm.AllowsSeparateAttackAndDefendCommandsPerTurn)
        {
            return !state.HasUsedDefendCommandThisTurn
                   && (!state.HasUsedCommandThisTurn || state.HasUsedAttackCommandThisTurn);
        }

        return !state.HasUsedCommandThisTurn;
    }

    /// <summary>
    /// After Command Attack or Command Defend resolves: one combined command for normal monsters, or separate slots until both are used.
    /// </summary>
    public static async Task CommitMonsterCommandAfterPlay(Creature pet, bool isAttackCommand, Creature? applier, NormalMonsterCard? sourceMonster)
    {
        if (sourceMonster is BaseMonsterCard bm && bm.AllowsSeparateAttackAndDefendCommandsPerTurn)
        {
            var state = GetOrCreate(pet);
            if (isAttackCommand)
                state.HasUsedAttackCommandThisTurn = true;
            else
                state.HasUsedDefendCommandThisTurn = true;

            if (state.HasUsedAttackCommandThisTurn && state.HasUsedDefendCommandThisTurn)
                await SetHasUsedCommandThisTurn(pet, true, applier, sourceMonster);
            return;
        }

        await SetHasUsedCommandThisTurn(pet, true, applier, sourceMonster);
    }

    /// <summary>True if this pet has used any Command Attack/Defend allowance this turn (including one of two dual slots).</summary>
    public static bool PetHasUsedAnyCommandSlotThisTurn(Creature pet)
    {
        var s = GetOrCreate(pet);
        return s.HasUsedCommandThisTurn || s.HasUsedAttackCommandThisTurn || s.HasUsedDefendCommandThisTurn;
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

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, sourceCard))
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
            if (CombatManager.Instance?.IsInProgress == true)
            {
                GD.Print(
                    $"[YgoDuelist][MP][DieForYou] SetDieForYouForcedAsync apply playerNetId={player.NetId} petCombatId={pet.CombatId} source={sourceCard?.Id?.Entry}");
            }

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

    /// <summary>D.D. Warrior / D.D. Warrior Lady: cleared at the start of your turn (with trap <c>SetThisTurn</c> bookkeeping).</summary>
    public static void ResetHasAttackedThisTurnForPlayerTurnStart(Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (TryGet(pet, out MonsterCommandState s))
                s.HasAttackedThisTurn = false;
        }
    }

    /// <summary>End of controlling player turn: per-field-monster cleanup (Karate Man, Guardian Slime) before <see cref="ClearPerTurnExtrasForPlayer"/>.</summary>
    public static async Task ResolveOwnerTurnEndFieldCleanupAsync(PlayerChoiceContext ctx, Player? player)
    {
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard card)
                await card.OnOwnerTurnEndFieldCleanupAsync(ctx, player, pet);
        }
    }

    /// <summary>End of player turn: Cyber Jar free commands and per-turn extras.</summary>
    public static void ClearPerTurnExtrasForPlayer(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!TryGet(pet, out MonsterCommandState s))
                continue;
            s.ZeroEnergyMonsterCommandsThisTurn = false;
            s.ExarionUniversePiercingStanceThisTurn = false;
            s.KarateManBurstAtkThisTurn = false;
            s.KarateManDestroyAtEndOfOwnerTurn = false;
            s.GuardianSlimeDestroyAtEndOfOwnerTurn = false;
            s.HasUsedSecondActivatedEffectThisTurn = false;
            s.GrayWingAtkPenaltyThisTurn = 0;
            s.GrayWingDoubleAttackThisTurn = false;
            s.TyrantDragonDoubleAttackThisTurn = false;
            s.CatapultTurtleActivatedThisTurn = false;
            s.GoddessOfWhimAtkMultiplierThisTurn = 1m;
        }

        foreach (BaseMonsterCard c in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
            c.ClearTurnEndFieldBuffsFromMonsterCommandRegistry();
    }

    /// <summary>Clears all command state. Call at end of combat.</summary>
    public static void ClearAll()
    {
        _states.Clear();
    }
}
