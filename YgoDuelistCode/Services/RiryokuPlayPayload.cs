using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class RiryokuPlayPayload
{
    private static readonly Dictionary<CardModel, (BaseMonsterCard Donor, BaseMonsterCard Receiver)> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel card, BaseMonsterCard donor, BaseMonsterCard receiver)
    {
        lock (Gate)
            Pending[card] = (donor, receiver);
    }

    public static bool TryTakePending(CardModel card, out BaseMonsterCard? donor, out BaseMonsterCard? receiver)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(card, out var pair))
            {
                donor = null;
                receiver = null;
                return false;
            }

            Pending.Remove(card);
            donor = pair.Donor;
            receiver = pair.Receiver;
            return true;
        }
    }

    public static void ClearForCard(CardModel card)
    {
        lock (Gate)
            Pending.Remove(card);
    }

    public static void ClearAll()
    {
        lock (Gate)
            Pending.Clear();
    }
}
