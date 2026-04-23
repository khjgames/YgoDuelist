using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// <see cref="Amount"/> is turns remaining (including the current turn). Each end of your turn, Machine duel monsters are destroyed, then stacks tick down.
/// </summary>
public sealed class LimiterRemovalPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-LIMITER_REMOVAL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-LIMITER_REMOVAL_POWER.description");

    /// <summary>Applied to summed ATK/DEF for your Machine monsters while this power is active.</summary>
    public StatEffectTotalMultiplier MachineDuelMonsterStatMultiplier => StatEffectTotalMultiplier.LimiterRemovalMachine;

    public static async Task RefreshLimitRemovedOnOwnerMachinePetsAsync(Player player)
    {
        if (player?.Creature == null || player.PlayerCombatState == null)
            return;
        if (!player.Creature.HasPower<LimiterRemovalPower>())
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard src
                || src.DuelMonsterRace != DuelMonsterRace.Machine)
                continue;
            await PowerCmd.Apply<LimitRemovedPower>(pet, 1m, player.Creature, null);
        }
    }

    public static async Task OnMachineDuelMonsterSummonedAsync(Player player, Creature petCreature, BaseMonsterCard card)
    {
        if (player?.Creature == null || petCreature == null)
            return;
        if (!player.Creature.HasPower<LimiterRemovalPower>())
            return;
        if (card.DuelMonsterRace != DuelMonsterRace.Machine)
            return;
        await PowerCmd.Apply<LimitRemovedPower>(petCreature, 1m, player.Creature, card);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        Player? player = Owner.Player;
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard src
                || src.DuelMonsterRace != DuelMonsterRace.Machine)
                continue;
            await CreatureCmd.Kill(pet, force: true);
        }

        await PowerCmd.Decrement(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        Player? player = oldOwner?.Player;
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            await PowerCmd.Remove<LimitRemovedPower>(pet);
    }
}
