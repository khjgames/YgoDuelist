using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Integer choice from a pre-play grid (e.g. spell mode). Used when the grid shows transient cards that do not survive until OnPlay.
/// </summary>
public static class YgoPrePlayOptionIdPayload
{
    private static readonly Dictionary<CardModel, int> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel source, int optionId)
    {
        lock (Gate)
            Pending[source] = optionId;
    }

    public static bool TryTakePending(CardModel source, out int optionId)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(source, out optionId))
                return false;

            Pending.Remove(source);
            return true;
        }
    }

    public static void ClearForCard(CardModel source)
    {
        lock (Gate)
            Pending.Remove(source);
    }
}
