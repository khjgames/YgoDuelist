using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public sealed class TributeSummonPendingResolution
{
    public TributeSummonPendingResolution(List<Creature> pets, int mausoleumHpTributes, int mausoleumHpLossTotal)
    {
        Pets = pets;
        MausoleumHpTributes = mausoleumHpTributes;
        MausoleumHpLossTotal = mausoleumHpLossTotal;
    }

    public List<Creature> Pets { get; }

    public int MausoleumHpTributes { get; }

    public int MausoleumHpLossTotal { get; }
}

/// <summary>
/// Holds tribute <see cref="Creature"/> pets chosen in the selection UI for the next
/// <see cref="YgoDuelist.YgoDuelistCode.Cards.Core.NormalMonsterCard.OnPlay"/> of this play.
/// Keyed by owner net id + combat card index (<see cref="YgoPlayPayloadNetKey"/>) so host and clients resolve the same entry.
/// </summary>
public static class TributeSummonPlayPayload
{
    private static readonly Dictionary<(ulong OwnerNetId, uint CombatCardIndex), TributeSummonPendingResolution> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(ulong ownerNetId, uint combatCardIndex, TributeSummonPendingResolution resolution)
    {
        lock (Gate)
            Pending[(ownerNetId, combatCardIndex)] = resolution;
    }

    /// <summary>Removes and returns pending tributes for the given net card key, if any.</summary>
    public static bool TryTakePending(ulong ownerNetId, uint combatCardIndex, out TributeSummonPendingResolution? resolution)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue((ownerNetId, combatCardIndex), out resolution))
                return false;
            Pending.Remove((ownerNetId, combatCardIndex));
            return true;
        }
    }

    /// <summary>Same as <see cref="TryTakePending(ulong, uint, out TributeSummonPendingResolution?)"/> using <see cref="YgoPlayPayloadNetKey.TryGetKey"/>.</summary>
    public static bool TryTakePendingForCard(CardModel card, out TributeSummonPendingResolution? resolution)
    {
        resolution = null;
        return YgoPlayPayloadNetKey.TryGetKey(card, out ulong oid, out uint idx) && TryTakePending(oid, idx, out resolution);
    }

    /// <summary>
    /// Manual <see cref="PlayCardAction"/> path: use the same <see cref="NetCombatCard.CombatCardIndex"/> as
    /// <c>SetPending</c> in the tribute play patch — avoids deriving the key from <c>NetCombatCard.FromModel</c> at
    /// <c>OnPlay</c> time (pile/state can make that fail on peers).
    /// </summary>
    public static bool TryTakePendingForManualPlay(PlayerChoiceContext? choiceContext, CardModel card, out TributeSummonPendingResolution? resolution)
    {
        resolution = null;
        if (choiceContext is GameActionPlayerChoiceContext { Action: PlayCardAction pca })
        {
            bool ok = TryTakePending(pca.Player.NetId, pca.NetCombatCard.CombatCardIndex, out resolution);
            if (!ok)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][Tribute] TryTake miss (PlayCardAction key): owner={pca.Player.NetId} netIdx={pca.NetCombatCard.CombatCardIndex} card={card.Id?.Entry}");
            }
            else
            {
                GD.Print(
                    $"[YgoDuelist][MP][Tribute] TryTake ok (PlayCardAction): owner={pca.Player.NetId} netIdx={pca.NetCombatCard.CombatCardIndex} pets={resolution?.Pets.Count ?? 0}");
            }

            return ok;
        }

        if (!TryTakePendingForCard(card, out resolution))
        {
            if (YgoPlayPayloadNetKey.TryGetKey(card, out ulong oid, out uint idx))
                GD.PrintErr($"[YgoDuelist][MP][Tribute] TryTake miss (card key): ({oid},{idx}) card={card.Id?.Entry}");
            else
                GD.PrintErr($"[YgoDuelist][MP][Tribute] TryTake miss (no NetCombatCard key) card={card.Id?.Entry}");
            return false;
        }

        GD.Print($"[YgoDuelist][MP][Tribute] TryTake ok (card key): card={card.Id?.Entry} pets={resolution?.Pets.Count ?? 0}");
        return true;
    }

    public static void ClearForKey(ulong ownerNetId, uint combatCardIndex)
    {
        lock (Gate)
            Pending.Remove((ownerNetId, combatCardIndex));
    }

    public static void ClearForCard(CardModel? card)
    {
        if (card == null || !YgoPlayPayloadNetKey.TryGetKey(card, out ulong oid, out uint idx))
            return;
        ClearForKey(oid, idx);
    }

    public static void ClearAll()
    {
        lock (Gate)
            Pending.Clear();
    }
}
