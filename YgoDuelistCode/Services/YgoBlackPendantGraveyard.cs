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
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Black_Pendant"/>: when this card is sent to the YGO Graveyard, apply <c>Mgc</c> <see cref="BlightPower"/> to a random enemy.</summary>
public static class YgoBlackPendantGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Black_Pendant pendant)
            return;
        if (pile.Type != GraveyardPile.CustomType || !pile.IsCombatPile)
            return;
        if (CombatManager.Instance is not { IsInProgress: true })
            return;
        CombatState? cs = CombatManager.Instance.DebugOnlyGetState();
        if (cs == null)
            return;

        Player? gyOwner = ResolveGraveyardOwner(cs, pile);
        if (gyOwner == null)
            gyOwner = addedCard.Owner;
        if (gyOwner?.Creature?.CombatState == null || gyOwner.Creature.Side != CombatSide.Player)
            return;

        Creature playerCreature = gyOwner.Creature;
        int stacks = (int)pendant.DynamicVars["Mgc"].BaseValue;
        if (stacks <= 0)
            return;

        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(gyOwner, pendant);
        string key = $"BLACK_PENDANT-{addedCard.Id}-{pile.Cards.Count}";

        TaskHelper.RunSafely(ApplyOnceAsync(cs, playerCreature, pendant, stacks, key, mix));
    }

    private static Player? ResolveGraveyardOwner(CombatState cs, CardPile pile)
    {
        foreach (Player p in cs.Players)
        {
            if (GraveyardRelic.GetGraveyardPile(p) == pile)
                return p;
        }

        return null;
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
