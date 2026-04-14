using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Solar_Flare_Dragon"/>: before end-of-turn flush, while you control another Pyro, inflict <c>Mgc2</c> Blight on a random enemy.
/// </summary>
public static class YgoSolarFlareDragonEndPhase
{
    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature?.CombatState == null)
            return;

        _ = choiceContext ?? new BlockingPlayerChoiceContext();

        foreach (Creature pet in player.PlayerCombatState.Pets.ToList())
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not Solar_Flare_Dragon solar || solar.FaceDown)
                continue;
            if (solar.Owner == null)
                continue;
            if (!HasOtherPyroOnField(player, solar))
                continue;

            int blight = (int)solar.DynamicVars["Mgc2"].BaseValue;
            if (blight <= 0)
                continue;

            List<Creature> enemies = player.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0)
                continue;

            Creature? target = YgoDeterministicRng.PickOne(
                player.Creature.CombatState,
                enemies,
                "SOLAR_FLARE_DRAGON_BLIGHT",
                (ulong)(pet.CombatId ?? 0u));
            if (target == null)
                continue;

            await PowerCmd.Apply<BlightPower>(target, blight, player.Creature, solar);
        }
    }

    private static bool HasOtherPyroOnField(Player player, Solar_Flare_Dragon self)
    {
        foreach (BaseMonsterCard? m in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (m == null || m.FaceDown || ReferenceEquals(m, self))
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Pyro)
                return true;
        }

        return false;
    }
}
