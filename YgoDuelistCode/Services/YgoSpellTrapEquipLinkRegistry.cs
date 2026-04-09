using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Combat-scoped links between zone cards implementing <see cref="IYgoSpellTrapEquipLink"/> and a field <see cref="BaseMonsterCard"/>.
/// </summary>
public static class YgoSpellTrapEquipLinkRegistry
{
    private static readonly Dictionary<BaseMonsterCard, List<CardModel>> ByMonster = new();
    private static readonly Dictionary<CardModel, BaseMonsterCard> TrapToMonster = new();
    private static readonly object Gate = new();

    public static void Attach(CardModel trapCard, BaseMonsterCard monster)
    {
        if (trapCard is not IYgoSpellTrapEquipLink link || monster == null)
            return;

        lock (Gate)
        {
            DetachUnsafe(trapCard);

            if (!ByMonster.TryGetValue(monster, out var list))
            {
                list = new List<CardModel>();
                ByMonster[monster] = list;
            }

            list.Add(trapCard);
            TrapToMonster[trapCard] = monster;
            link.SetEquipLinkedMonster(monster);
            link.SetEquipLinkedPetCombatId(YgoDuelMonsterPetBinding.TryFindPetCombatIdForFieldMonster(monster));
        }
    }

    public static void Detach(CardModel trapCard)
    {
        if (trapCard == null)
            return;
        lock (Gate)
            DetachUnsafe(trapCard);
    }

    private static void DetachUnsafe(CardModel trapCard)
    {
        if (trapCard is not IYgoSpellTrapEquipLink link)
            return;

        if (!TrapToMonster.TryGetValue(trapCard, out var monster))
            return;

        TrapToMonster.Remove(trapCard);
        link.SetEquipLinkedPetCombatId(0);
        link.SetEquipLinkedMonster(null);

        if (ByMonster.TryGetValue(monster, out var list))
        {
            list.Remove(trapCard);
            if (list.Count == 0)
                ByMonster.Remove(monster);
        }
    }

    /// <summary>Removes registry entry and returns the linked monster (for destroying the monster when the trap leaves the field).</summary>
    public static BaseMonsterCard? DetachAndConsumeLinkedMonster(CardModel trapCard)
    {
        if (trapCard is not IYgoSpellTrapEquipLink)
            return null;

        lock (Gate)
        {
            if (!TrapToMonster.TryGetValue(trapCard, out var monster))
                return null;

            DetachUnsafe(trapCard);
            return monster;
        }
    }

    public static IReadOnlyList<CardModel> GetLinkedTrapsForMonster(BaseMonsterCard? monster)
    {
        if (monster == null)
            return System.Array.Empty<CardModel>();

        lock (Gate)
        {
            return ByMonster.TryGetValue(monster, out var list)
                ? list.ToArray()
                : System.Array.Empty<CardModel>();
        }
    }

    public static BaseMonsterCard? GetLinkedMonster(CardModel? trapCard)
    {
        if (trapCard == null)
            return null;
        TryRebindEquipLinkIfNeeded(trapCard);
        lock (Gate)
            return TrapToMonster.TryGetValue(trapCard, out var m) ? m : null;
    }

    /// <summary>
    /// MP: restores registry + <see cref="IYgoSpellTrapEquipLink.SetEquipLinkedMonster"/> from <see cref="IYgoSpellTrapEquipLink.EquipLinkedPetCombatId"/>.
    /// </summary>
    public static void TryRebindEquipLinkIfNeeded(CardModel trapCard)
    {
        if (trapCard is not IYgoSpellTrapEquipLink link)
            return;
        Player? player = trapCard.Owner;
        if (player?.PlayerCombatState == null)
            return;
        if (link.EquipLinkedPetCombatId == 0)
            return;
        BaseMonsterCard? m = YgoDuelMonsterPetBinding.TryGetFieldMonsterForPetCombatId(player, link.EquipLinkedPetCombatId);
        if (m == null)
            return;
        Attach(trapCard, m);
    }

    public static void ClearAll()
    {
        lock (Gate)
        {
            foreach (var kv in TrapToMonster)
            {
                if (kv.Key is IYgoSpellTrapEquipLink link)
                {
                    link.SetEquipLinkedPetCombatId(0);
                    link.SetEquipLinkedMonster(null);
                }
            }

            ByMonster.Clear();
            TrapToMonster.Clear();
        }
    }

    /// <summary>Detach every equip-link trap for this monster without moving piles (caller moves cards).</summary>
    public static IReadOnlyList<CardModel> TakeAllLinksFromMonster(BaseMonsterCard monster)
    {
        if (monster == null)
            return System.Array.Empty<CardModel>();

        lock (Gate)
        {
            if (!ByMonster.TryGetValue(monster, out var list) || list.Count == 0)
                return System.Array.Empty<CardModel>();

            var copy = list.ToArray();
            foreach (var trap in copy)
            {
                TrapToMonster.Remove(trap);
                if (trap is IYgoSpellTrapEquipLink link)
                {
                    link.SetEquipLinkedPetCombatId(0);
                    link.SetEquipLinkedMonster(null);
                }
            }

            ByMonster.Remove(monster);
            return copy;
        }
    }
}
