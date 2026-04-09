using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Two field monsters chosen for Obelisk the Tormentor's activated effect.</summary>
public static class ObeliskActivatedTributePayload
{
    private static readonly Dictionary<NormalMonsterCard, List<BaseMonsterCard>> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(NormalMonsterCard source, List<BaseMonsterCard> tributes)
    {
        lock (Gate)
            Pending[source] = tributes;
    }

    public static bool TryTakePending(NormalMonsterCard source, out List<BaseMonsterCard>? tributes)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(source, out tributes))
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
