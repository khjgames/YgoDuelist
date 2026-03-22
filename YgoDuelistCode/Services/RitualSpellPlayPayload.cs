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
    private static readonly Dictionary<CardModel, RitualSpellPendingResolution> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel spell, RitualSpellPendingResolution resolution)
    {
        lock (Gate)
            Pending[spell] = resolution;
    }

    public static bool TryTakePending(CardModel spell, out RitualSpellPendingResolution? resolution)
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
