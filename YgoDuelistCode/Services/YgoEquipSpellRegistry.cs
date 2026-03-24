using System.Collections.Generic;
using System.Linq;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tracks which equip spell cards are attached to which field monsters (combat-scoped).
/// </summary>
public static class YgoEquipSpellRegistry
{
    private static readonly Dictionary<BaseMonsterCard, List<BaseEquipSpellCard>> ByMonster = new();
    private static readonly Dictionary<BaseEquipSpellCard, BaseMonsterCard> EquipToMonster = new();
    private static readonly object Gate = new();

    public static void Attach(BaseEquipSpellCard equip, BaseMonsterCard monster)
    {
        if (equip == null || monster == null)
            return;

        lock (Gate)
        {
            DetachUnsafe(equip);

            if (!ByMonster.TryGetValue(monster, out var list))
            {
                list = new List<BaseEquipSpellCard>();
                ByMonster[monster] = list;
            }

            list.Add(equip);
            EquipToMonster[equip] = monster;
            equip.SetEquippedMonster(monster);
        }
    }

    public static void Detach(BaseEquipSpellCard equip)
    {
        if (equip == null)
            return;
        lock (Gate)
            DetachUnsafe(equip);
    }

    private static void DetachUnsafe(BaseEquipSpellCard equip)
    {
        if (!EquipToMonster.TryGetValue(equip, out var monster))
            return;

        EquipToMonster.Remove(equip);
        equip.SetEquippedMonster(null);

        if (ByMonster.TryGetValue(monster, out var list))
        {
            list.Remove(equip);
            if (list.Count == 0)
                ByMonster.Remove(monster);
        }
    }

    public static IReadOnlyList<BaseEquipSpellCard> GetEquipsForMonster(BaseMonsterCard? monster)
    {
        if (monster == null)
            return System.Array.Empty<BaseEquipSpellCard>();

        lock (Gate)
        {
            return ByMonster.TryGetValue(monster, out var list)
                ? list.ToArray()
                : System.Array.Empty<BaseEquipSpellCard>();
        }
    }

    public static BaseMonsterCard? GetEquippedMonster(BaseEquipSpellCard? equip)
    {
        if (equip == null)
            return null;
        lock (Gate)
            return EquipToMonster.TryGetValue(equip, out var m) ? m : null;
    }

    public static void ClearAll()
    {
        lock (Gate)
        {
            foreach (var equip in EquipToMonster.Keys)
                equip.SetEquippedMonster(null);
            ByMonster.Clear();
            EquipToMonster.Clear();
        }
    }

    /// <summary>Detach every equip on this monster without moving piles (caller moves cards).</summary>
    public static IReadOnlyList<BaseEquipSpellCard> TakeAllEquipsFromMonster(BaseMonsterCard monster)
    {
        if (monster == null)
            return System.Array.Empty<BaseEquipSpellCard>();

        lock (Gate)
        {
            if (!ByMonster.TryGetValue(monster, out var list) || list.Count == 0)
                return System.Array.Empty<BaseEquipSpellCard>();

            var copy = list.ToArray();
            foreach (var eq in copy)
            {
                EquipToMonster.Remove(eq);
                eq.SetEquippedMonster(null);
            }

            ByMonster.Remove(monster);
            return copy;
        }
    }
}
