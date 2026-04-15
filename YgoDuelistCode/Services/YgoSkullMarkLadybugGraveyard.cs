using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Skull_Mark_Ladybug"/>: when sent to the YGO Graveyard, heal <c>Mgc</c> and gain <c>Mgc</c> <see cref="DoomPower"/> (same pattern as vanilla <c>BorrowedTime</c>).</summary>
public static class YgoSkullMarkLadybugGraveyard
{
    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Skull_Mark_Ladybug ladybug)
            return;
        if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? gyOwner))
            return;

        TaskHelper.RunSafely(ResolveAsync(gyOwner, ladybug));
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
