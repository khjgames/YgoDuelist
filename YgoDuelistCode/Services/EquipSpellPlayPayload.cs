using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Monster target chosen in <see cref="Patches.PlayCardActionEquipSpellPatch"/> before resources are spent.
/// </summary>
public static class EquipSpellPlayPayload
{
    private static readonly Dictionary<CardModel, BaseMonsterCard> Pending = new();
    private static readonly object Gate = new();

    public static void SetPending(CardModel equipSpell, BaseMonsterCard targetMonster)
    {
        lock (Gate)
            Pending[equipSpell] = targetMonster;
    }

    public static bool TryTakePending(CardModel equipSpell, out BaseMonsterCard? targetMonster)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue(equipSpell, out targetMonster))
                return false;
            Pending.Remove(equipSpell);
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
