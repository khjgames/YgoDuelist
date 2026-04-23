using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Skull_Invitation"/>: each card added to the YGO Graveyard deals <c>Mgc</c> to a random enemy.</summary>
public static class YgoSkullInvitationGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? gyOwner))
            return;

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(gyOwner);
        Skull_Invitation? inv = zone == null
            ? null
            : YgoMpCombatOrder.FirstCardWhereStable(
                zone.Cards,
                c => c is Skull_Invitation trap && !trap.FaceDown) as Skull_Invitation;
        if (inv == null)
            return;

        decimal dmg = inv.DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(gyOwner, inv);
        string key = $"SKULL_INVITATION-{addedCard.Id}-{pile.Cards.Count}";

        TaskHelper.RunSafely(DealOnceAsync(gyOwner, inv, dmg, key, mix));
    }

    private static async Task DealOnceAsync(Player player, Skull_Invitation inv, decimal dmg, string rngKey, ulong mix)
    {
        var ctx = YgoChoiceContexts.Blocking();
        CombatState? cs = player.Creature?.CombatState;
        if (cs == null)
            return;

        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);
        if (enemies.Count == 0)
            return;

        Creature? victim = YgoDeterministicRng.PickOne(cs, enemies, rngKey, mix);
        if (victim == null || !victim.IsAlive)
            return;

        await CreatureCmd.Damage(ctx, victim, dmg, ValueProp.Unpowered, player.Creature!, inv);
    }
}
