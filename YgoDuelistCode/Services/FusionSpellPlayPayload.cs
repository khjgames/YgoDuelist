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
    private static readonly Dictionary<CardModel, FusionSpellPendingResolution> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel spell, FusionSpellPendingResolution resolution)
    {
        lock (Gate)
            Pending[spell] = resolution;
    }

    public static bool TryTakePending(CardModel spell, out FusionSpellPendingResolution? resolution)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(spell, out resolution))
                return false;
            Pending.Remove(spell);
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
