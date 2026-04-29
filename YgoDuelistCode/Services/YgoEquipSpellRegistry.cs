using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

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
            equip.SetEquippedTargetPetCombatId(YgoDuelMonsterPetBinding.TryFindPetCombatIdForFieldMonster(monster));
            equip.OnAfterAttachedToFieldMonster(monster);
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
        equip.SetEquippedTargetPetCombatId(0);

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
        equip.TryResolveEquippedMonsterFromStoredPetId();
        lock (Gate)
            return EquipToMonster.TryGetValue(equip, out var m) ? m : null;
    }

    public static void ClearAll()
    {
        lock (Gate)
        {
            foreach (BaseEquipSpellCard equip in YgoMpCombatOrder.CardsOrderedForMp(EquipToMonster.Keys).OfType<BaseEquipSpellCard>())
            {
                equip.SetEquippedMonster(null);
                equip.SetEquippedTargetPetCombatId(0);
            }
            ByMonster.Clear();
            EquipToMonster.Clear();
        }
    }

    /// <summary>
    /// MP: before <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetFullCombatState"/> checksum snapshots, align this registry
    /// with face-up equips in the spell/trap zone and live field monsters (<see cref="YgoDuelMonsterPetBinding"/>). Drops stale rows
    /// when the card left the zone or the field pet is gone; re-attaches when the stored pet id points at a different mapping.
    /// </summary>
    public static (int Detached, int Rebound) ReconcileOrphansBeforeMpChecksum(IRunState runState)
    {
        int detached = 0;
        int rebound = 0;

        foreach (Player player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(runState.Players))
        {
            if (player?.Creature == null)
                continue;

            CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
            if (zone == null)
                continue;

            foreach (CardModel c in zone.Cards.ToList())
            {
                if (c is not BaseEquipSpellCard eq || eq.FaceDown || eq.EquippedTargetPetCombatId == 0u)
                    continue;
                eq.TryResolveEquippedMonsterFromStoredPetId();
            }
        }

        lock (Gate)
        {
            foreach (BaseEquipSpellCard equip in YgoMpCombatOrder.CardsOrderedForMp(EquipToMonster.Keys).OfType<BaseEquipSpellCard>())
            {
                if (equip.Owner == null)
                {
                    DetachUnsafe(equip);
                    detached++;
                    continue;
                }

                if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                {
                    DetachUnsafe(equip);
                    detached++;
                    continue;
                }

                if (equip.EquippedTargetPetCombatId == 0u)
                {
                    DetachUnsafe(equip);
                    detached++;
                    continue;
                }

                BaseMonsterCard? expected = YgoDuelMonsterPetBinding.TryGetFieldMonsterForPetCombatId(equip.Owner, equip.EquippedTargetPetCombatId);
                if (expected == null)
                {
                    DetachUnsafe(equip);
                    detached++;
                    continue;
                }

                if (!EquipToMonster.TryGetValue(equip, out BaseMonsterCard? mapped) || !ReferenceEquals(mapped, expected))
                {
                    Attach(equip, expected);
                    rebound++;
                }
            }
        }

        return (detached, rebound);
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
                eq.SetEquippedTargetPetCombatId(0);
            }

            ByMonster.Remove(monster);
            return copy;
        }
    }
}
