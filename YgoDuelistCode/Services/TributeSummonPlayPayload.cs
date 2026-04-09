using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
/// <see cref="YgoDuelist.YgoDuelistCode.Cards.Core.NormalMonsterCard.OnPlay"/> of this card instance.
/// </summary>
public static class TributeSummonPlayPayload
{
    private static readonly Dictionary<CardModel, TributeSummonPendingResolution> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel card, TributeSummonPendingResolution resolution)
    {
        lock (Gate)
            Pending[card] = resolution;
    }

    /// <summary>Removes and returns pending tributes for <paramref name="card"/>, if any.</summary>
    public static bool TryTakePending(CardModel card, out TributeSummonPendingResolution? resolution)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(card, out resolution))
                return false;
            Pending.Remove(card);
            return true;
        }
    }

    public static void ClearForCard(CardModel? card)
    {
        if (card == null)
            return;
        lock (Gate)
            Pending.Remove(card);
    }

    public static void ClearAll()
    {
        lock (Gate)
            Pending.Clear();
    }
}
