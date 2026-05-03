using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Links a Union-Effect monster card in Limbo to its union equip spell on the field, plus HP snapshot for unequip.
/// </summary>
public static class YgoUnionLimboRegistry
{
    private sealed class Pair
    {
        public BaseMonsterCard? MonsterInLimbo;
        public BaseEquipSpellCard? EquipInZone;
        public int SnapshotHp;
        public int SnapshotMaxHp;
    }

    private static readonly Dictionary<BaseMonsterCard, Pair> ByMonster = new();
    private static readonly Dictionary<BaseEquipSpellCard, Pair> ByEquip = new();
    private static readonly object Gate = new();

    public static void ClearAll()
    {
        lock (Gate)
        {
            ByMonster.Clear();
            ByEquip.Clear();
        }
    }

    public static void RegisterPair(
        BaseMonsterCard monsterInLimbo,
        BaseEquipSpellCard equipInZone,
        int snapshotHp,
        int snapshotMaxHp)
    {
        if (monsterInLimbo == null || equipInZone == null)
            return;
        lock (Gate)
        {
            var p = new Pair
            {
                MonsterInLimbo = monsterInLimbo,
                EquipInZone = equipInZone,
                SnapshotHp = snapshotHp,
                SnapshotMaxHp = snapshotMaxHp,
            };
            ByMonster[monsterInLimbo] = p;
            ByEquip[equipInZone] = p;
        }
    }

    public static bool TryGetByEquip(BaseEquipSpellCard equip, out BaseMonsterCard? monster, out int snapshotHp, out int snapshotMaxHp)
    {
        monster = null;
        snapshotHp = 0;
        snapshotMaxHp = 0;
        lock (Gate)
        {
            if (!ByEquip.TryGetValue(equip, out Pair? p))
                return false;
            monster = p.MonsterInLimbo;
            snapshotHp = p.SnapshotHp;
            snapshotMaxHp = p.SnapshotMaxHp;
            return true;
        }
    }

    public static bool TryGetByMonsterInLimbo(BaseMonsterCard monster, out BaseEquipSpellCard? equip)
    {
        equip = null;
        lock (Gate)
        {
            if (!ByMonster.TryGetValue(monster, out Pair? p))
                return false;
            equip = p.EquipInZone;
            return equip != null;
        }
    }

    public static void RemovePairForEquip(BaseEquipSpellCard equip)
    {
        if (equip == null)
            return;
        lock (Gate)
        {
            if (!ByEquip.TryGetValue(equip, out Pair? p))
                return;
            ByEquip.Remove(equip);
            if (p.MonsterInLimbo != null)
                ByMonster.Remove(p.MonsterInLimbo);
        }
    }

    public static void UpdateEquipReference(BaseMonsterCard monsterInLimbo, BaseEquipSpellCard newEquip)
    {
        lock (Gate)
        {
            if (!ByMonster.TryGetValue(monsterInLimbo, out Pair? p))
                return;
            if (p.EquipInZone != null)
                ByEquip.Remove(p.EquipInZone);
            p.EquipInZone = newEquip;
            ByEquip[newEquip] = p;
        }
    }

    public static void ReconcileForMpChecksumSnapshot(IRunState runState)
    {
        List<BaseEquipSpellCard> toRemove = new();
        lock (Gate)
        {
            foreach (BaseEquipSpellCard eq in ByEquip.Keys.ToList())
            {
                if (eq is not IYgoUnionEquipSpell)
                {
                    toRemove.Add(eq);
                    continue;
                }

                if (eq.Owner is not Player pl)
                {
                    toRemove.Add(eq);
                    continue;
                }

                CardPile? zone = YgoPlayerPiles.SpellTrapZone(pl);
                CardPile? limbo = YgoPlayerPiles.Limbo(pl);
                if (zone == null || limbo == null)
                {
                    toRemove.Add(eq);
                    continue;
                }

                if (!ByEquip.TryGetValue(eq, out Pair? p) || p.MonsterInLimbo == null)
                {
                    toRemove.Add(eq);
                    continue;
                }

                bool equipInZone = eq.Pile?.Type == SpellTrapZonePile.CustomType && !eq.FaceDown && zone.Cards.Contains(eq);
                bool equipInLimbo = eq.Pile?.Type == LimboPile.CustomType && limbo.Cards.Contains(eq);
                if (!equipInZone && !equipInLimbo)
                {
                    toRemove.Add(eq);
                    continue;
                }

                bool monInLimbo = p.MonsterInLimbo.Pile?.Type == LimboPile.CustomType && limbo.Cards.Contains(p.MonsterInLimbo);
                bool monOnField = p.MonsterInLimbo.Pile?.Type == MonsterPile.CustomType;
                if (!monInLimbo && !monOnField)
                {
                    toRemove.Add(eq);
                    continue;
                }

                if (equipInZone && !monInLimbo)
                    toRemove.Add(eq);
            }

            foreach (BaseEquipSpellCard eq in toRemove)
            {
                if (!ByEquip.TryGetValue(eq, out Pair? p))
                    continue;
                ByEquip.Remove(eq);
                if (p.MonsterInLimbo != null)
                    ByMonster.Remove(p.MonsterInLimbo);
            }
        }

        _ = runState;
    }
}