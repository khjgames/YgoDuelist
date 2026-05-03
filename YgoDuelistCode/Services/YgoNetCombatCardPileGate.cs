using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Vanilla <see cref="CardPile.IsCombatPile"/> is <c>Type.IsCombatPile()</c>, which is only true for draw/hand/discard/exhaust/play.
/// YGO zones use custom <see cref="PileType"/> values, so they are never "combat piles" by that flag — yet any mutable card there
/// must receive a <see cref="NetCombatCardDb"/> id the same way as hand cards.
/// </summary>
public static class YgoNetCombatCardPileGate
{
    public static bool ShouldIdMutableCardOnPileAdd(CardPile pile) =>
        pile.IsCombatPile || pile is MonsterPile or YgoCardOptionPile or SpellTrapZonePile or GraveyardPile or FieldPile
            or ExtraDeckPile or BanishedPile or LimboPile;

    /// <summary>
    /// Call before any <see cref="NetCombatCardDb.GetCardId"/> sort/compare on live <see cref="CardModel"/> instances
    /// (e.g. tribute/fusion feasibility, hand grids). Covers edge cases where a card was not yet registered after
    /// <see cref="CardPile.AddInternal"/> postfix order vs. immediate UI/query paths.
    /// </summary>
    public static void EnsureMutableCombatCardsHaveNetIds(IEnumerable<CardModel?> cards)
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;
        var db = NetCombatCardDb.Instance;
        foreach (CardModel? c in cards)
        {
            if (c == null || !c.IsMutable)
                continue;
            if (db.TryGetCardId(c, out _))
                continue;
            GD.PrintErr(
                $"[YgoDuelist][MP][NetCard] EnsureMutableCombatCardsHaveNetIds: assigning missing combat id entry={c.Id?.Entry} type={c.GetType().Name}");
            db.IdCardForTesting(c);
        }
    }
}
