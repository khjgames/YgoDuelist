using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Ritual target + materials chosen in the grid UI; consumed during <see cref="RitualSpellCard"/> spell resolution (<see cref="BaseSpellCard.OnPlay"/>).
/// </summary>
public sealed class RitualSpellPendingResolution
{
    public RitualMonsterCard RitualTarget { get; }
    public List<BaseMonsterCard> Materials { get; }

    public RitualSpellPendingResolution(RitualMonsterCard ritualTarget, List<BaseMonsterCard> materials)
    {
        RitualTarget = ritualTarget;
        Materials = materials;
    }
}

public static class RitualSpellPlayPayload
{
    private static readonly Dictionary<(ulong OwnerNetId, uint CombatCardIndex), RitualSpellPendingResolution> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(ulong ownerNetId, uint combatCardIndex, RitualSpellPendingResolution resolution)
    {
        lock (Gate)
            Pending[(ownerNetId, combatCardIndex)] = resolution;
    }

    public static bool TryTakePending(ulong ownerNetId, uint combatCardIndex, out RitualSpellPendingResolution? resolution)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue((ownerNetId, combatCardIndex), out resolution))
                return false;
            Pending.Remove((ownerNetId, combatCardIndex));
            return true;
        }
    }

    public static bool TryTakePendingForCard(CardModel spell, out RitualSpellPendingResolution? resolution)
    {
        resolution = null;
        return YgoPlayPayloadNetKey.TryGetKey(spell, out ulong oid, out uint idx) && TryTakePending(oid, idx, out resolution);
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
