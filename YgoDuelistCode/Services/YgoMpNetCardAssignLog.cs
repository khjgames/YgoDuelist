using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Optional MP tracing: <see cref="NetCombatCardDb"/> assigns sequential uints as cards enter combat piles. If any peer
/// runs a different pile mutation order, indices diverge and <see cref="NetCombatCard"/> in net messages point at the
/// wrong <see cref="CardModel"/>. Set <see cref="Verbose"/> to true (e.g. from <c>MainFile.Initialize</c>) while
/// reproducing desyncs — logs can be very large.
/// </summary>
public static class YgoMpNetCardAssignLog
{
    /// <summary>When true, logs every mutable card added to a combat pile after id assignment.</summary>
    public static bool Verbose;

    public static void AfterIdAssigned(CardPile pile, CardModel card)
    {
        if (!Verbose || pile == null || card == null || !card.IsMutable)
            return;
        if (!pile.IsCombatPile)
            return;

        ulong owner = card.Owner?.NetId ?? 0;
        string entry = card.Id?.Entry ?? "?";
        try
        {
            uint idx = NetCombatCard.FromModel(card).CombatCardIndex;
            GD.Print(
                $"[YgoDuelist][MP][NetCard] add ownerNet={owner} pile={(int)pile.Type} card={entry} combatCardIdx={idx}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr(
                $"[YgoDuelist][MP][NetCard] add ownerNet={owner} pile={(int)pile.Type} card={entry} — no NetCombatCard: {ex.Message}");
        }
    }
}
