using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class ActivatedEffectTributeSelectionPayload
{
    private static readonly Dictionary<NormalMonsterCard, BaseMonsterCard> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(NormalMonsterCard source, BaseMonsterCard tribute)
    {
        lock (Gate)
            Pending[source] = tribute;
    }

    public static bool TryTakePending(NormalMonsterCard source, out BaseMonsterCard? tribute)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(source, out tribute))
                return false;

            Pending.Remove(source);
            return true;
        }
    }

    public static void ClearForSource(NormalMonsterCard source)
    {
        lock (Gate)
            Pending.Remove(source);
    }

    public static void ClearAll()
    {
        lock (Gate)
            Pending.Clear();
    }
}
