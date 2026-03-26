using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class RushReliablePlayPayload
{
    private static readonly Dictionary<CardModel, BaseMonsterCard> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel card, BaseMonsterCard target)
    {
        lock (Gate)
            Pending[card] = target;
    }

    public static bool TryTakePending(CardModel card, out BaseMonsterCard? target)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(card, out target))
                return false;

            Pending.Remove(card);
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
