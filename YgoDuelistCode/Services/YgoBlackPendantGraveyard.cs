using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Black_Pendant"/>: when this card is sent to the YGO Graveyard, apply <c>Mgc</c> <see cref="BlightPower"/> to a random enemy.</summary>
public static class YgoBlackPendantGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Black_Pendant pendant)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? gyOwner))
            return;

        Creature playerCreature = gyOwner.Creature;
        CombatState? cs = playerCreature.CombatState;
        if (cs == null)
            return;

        int stacks = (int)pendant.DynamicVars["Mgc"].BaseValue;
        if (stacks <= 0)
            return;

        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(gyOwner, pendant);
        string key = $"BLACK_PENDANT-{addedCard.Id}-{pile.Cards.Count}";

        TaskHelper.RunSafely(ApplyOnceAsync(cs, playerCreature, pendant, stacks, key, mix));
    }

    private static async Task ApplyOnceAsync(
        CombatState cs,
        Creature playerCreature,
        Black_Pendant pendant,
        int stacks,
        string rngKey,
        ulong mix)
    {
        List<Creature> enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
            return;

        Creature? victim = YgoDeterministicRng.PickOne(cs, enemies, rngKey, mix);
        if (victim == null || !victim.IsAlive)
            return;

        await PowerCmd.Apply<BlightPower>(victim, stacks, playerCreature, pendant);
    }
}
