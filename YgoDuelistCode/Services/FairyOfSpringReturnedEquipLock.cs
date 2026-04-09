using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Equip spells returned by Fairy of the Spring cannot be activated for the rest of that turn (unupgraded).</summary>
public static class FairyOfSpringReturnedEquipLock
{
    private static readonly List<CardModel> Locked = new();

    public static void Mark(CardModel equipSpell)
    {
        if (equipSpell == null)
            return;
        foreach (CardModel c in Locked)
        {
            if (ReferenceEquals(c, equipSpell))
                return;
        }

        Locked.Add(equipSpell);
    }

    public static bool IsLocked(CardModel? equipSpell)
    {
        if (equipSpell == null)
            return false;
        foreach (CardModel c in Locked)
        {
            if (ReferenceEquals(c, equipSpell))
                return true;
        }

        return false;
    }

    public static void ClearAll() => Locked.Clear();
}
