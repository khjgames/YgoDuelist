using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public sealed class FusionSpellPendingResolution
{
    public FusionMonsterCard FusionTarget { get; }
    public List<BaseMonsterCard> Materials { get; }

    public FusionSpellPendingResolution(FusionMonsterCard fusionTarget, List<BaseMonsterCard> materials)
    {
        FusionTarget = fusionTarget;
        Materials = materials;
    }
}

public static class FusionSpellPlayPayload
{
    private static readonly Dictionary<(ulong OwnerNetId, uint CombatCardIndex), FusionSpellPendingResolution> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(ulong ownerNetId, uint combatCardIndex, FusionSpellPendingResolution resolution)
    {
        lock (Gate)
            Pending[(ownerNetId, combatCardIndex)] = resolution;
    }

    public static bool TryTakePending(ulong ownerNetId, uint combatCardIndex, out FusionSpellPendingResolution? resolution)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue((ownerNetId, combatCardIndex), out resolution))
                return false;
            Pending.Remove((ownerNetId, combatCardIndex));
            return true;
        }
    }

    public static bool TryTakePendingForCard(CardModel spell, out FusionSpellPendingResolution? resolution)
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
