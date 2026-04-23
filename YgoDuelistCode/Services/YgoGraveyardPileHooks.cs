using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared guards and owner resolution for handlers that react to cards entering the YGO graveyard pile.
/// Keep patches on this generic dispatch path instead of re-adding per-card graveyard service calls.
/// </summary>
public static class YgoGraveyardPileHooks
{
    /// <summary>True when <paramref name="pile"/> is the combat graveyard and <paramref name="addedCard"/> belongs to the player side in an active combat.</summary>
    public static bool TryGetPlayerForGraveyardAdd(
        CardPile pile,
        CardModel addedCard,
        [NotNullWhen(true)] out Player? player)
    {
        player = null;
        if (pile.Type != GraveyardPile.CustomType || !pile.IsCombatPile)
            return false;
        if (CombatManager.Instance is not { IsInProgress: true })
            return false;
        CombatState? cs = CombatManager.Instance.DebugOnlyGetState();
        if (cs == null)
            return false;

        player = ResolveGraveyardOwner(cs, pile) ?? addedCard.Owner;
        if (player?.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
        {
            player = null;
            return false;
        }

        return true;
    }

    public static Player? ResolveGraveyardOwner(CombatState cs, CardPile pile)
    {
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            if (YgoPlayerPiles.Graveyard(p) == pile)
                return p;
        }

        return null;
    }

    /// <summary>
    /// Dispatches card-owned graveyard hooks after owner resolution succeeds.
    /// </summary>
    public static void DispatchCardAddedHook(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not IYgoOnAddedToYgoGraveyardPile hook)
            return;
        if (!TryGetPlayerForGraveyardAdd(pile, addedCard, out Player? owner))
            return;

        TaskHelper.RunSafely(hook.OnAddedToYgoGraveyardPileAsync(owner, pile));
    }
}
