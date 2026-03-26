using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class EmergencyProvisionsPlayPayload
{
    private static readonly Dictionary<CardModel, List<CardModel>> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel card, IEnumerable<CardModel> selectedCards)
    {
        lock (Gate)
            Pending[card] = selectedCards.Distinct().ToList();
    }

    public static bool TryTakePending(CardModel card, out List<CardModel>? selectedCards)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(card, out selectedCards))
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
