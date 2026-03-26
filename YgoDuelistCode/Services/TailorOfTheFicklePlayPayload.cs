using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

public sealed record TailorOfTheFicklePendingResolution(BaseEquipSpellCard Equip, BaseMonsterCard Target);

public static class TailorOfTheFicklePlayPayload
{
    private static readonly Dictionary<CardModel, TailorOfTheFicklePendingResolution> _pending = new();

    public static void SetPending(CardModel card, TailorOfTheFicklePendingResolution resolution)
    {
        _pending[card] = resolution;
    }

    public static bool TryTakePending(CardModel card, out TailorOfTheFicklePendingResolution? resolution)
    {
        if (_pending.TryGetValue(card, out var pending))
        {
            _pending.Remove(card);
            resolution = pending;
            return true;
        }

        resolution = null;
        return false;
    }

    public static void ClearForCard(CardModel card)
    {
        _pending.Remove(card);
    }

    public static void ClearAll()
    {
        _pending.Clear();
    }
}
