using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Skull_Mark_Ladybug"/>: when sent to the YGO Graveyard, heal <c>Mgc</c> and gain <c>Mgc</c> <see cref="DoomPower"/> (same pattern as vanilla <c>BorrowedTime</c>).</summary>
public static class YgoSkullMarkLadybugGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Skull_Mark_Ladybug ladybug)
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

        TaskHelper.RunSafely(ResolveAsync(gyOwner, ladybug));
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

    private static async Task ResolveAsync(Player player, Skull_Mark_Ladybug ladybug)
    {
        Creature? c = player.Creature;
        if (c == null || !c.IsAlive)
            return;

        decimal n = ladybug.DynamicVars["Mgc"].BaseValue;
        if (n <= 0m)
            return;

        await CreatureCmd.Heal(c, n);
        await PowerCmd.Apply<DoomPower>(c, n, c, ladybug);
    }
}
