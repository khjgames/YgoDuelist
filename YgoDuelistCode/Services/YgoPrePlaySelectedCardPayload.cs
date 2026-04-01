using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Single-card target chosen during pre-play grid (keyed by the spell/trap being played).</summary>
public static class YgoPrePlaySelectedCardPayload
{
    private static readonly Dictionary<CardModel, CardModel> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel source, CardModel selected)
    {
        lock (Gate)
            Pending[source] = selected;
    }

    public static bool TryTakePending(CardModel source, out CardModel? selected)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(source, out CardModel? value))
            {
                selected = null;
                return false;
            }

            Pending.Remove(source);
            selected = value;
            return true;
        }
    }

    public static void ClearForCard(CardModel source)
    {
        lock (Gate)
            Pending.Remove(source);
    }
}
