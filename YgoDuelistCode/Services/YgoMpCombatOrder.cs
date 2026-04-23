using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Single place for deterministic pet/zone ordering in combat MP (see <c>Game_Design/Multiplayer_playbook.md</c>).
/// Prefer these over raw <c>Pets.FirstOrDefault</c>, <c>Pets.Any</c> on live lists, <c>foreach</c> on live <c>PlayerCombatState.Pets</c>, or unstable
/// <c>zone.Cards.FirstOrDefault</c> when multiple instances can match or the loop body can mutate the pet list.
/// </summary>
public static class YgoMpCombatOrder
{
    public static IEnumerable<Creature> PetsOrderedByCombatId(PlayerCombatState pcs) =>
        PetsSnapshotOrderedByCombatId(pcs);

    /// <summary>
    /// Copy of live pets for <c>foreach</c>: avoids collection-modified during iteration and fixes processing order across peers (MP).
    /// </summary>
    public static List<Creature> PetsSnapshotOrderedByCombatId(PlayerCombatState? pcs) =>
        pcs == null ? new List<Creature>() : pcs.Pets.OrderBy(p => p.CombatId).ToList();

    /// <summary>
    /// Stable player iteration for combat/resolution passes that fan out across all players.
    /// Ordered by <see cref="Player.NetId"/> to avoid run/list/hash order drift between peers.
    /// </summary>
    public static List<Player> PlayersSnapshotOrderedByNetId(IEnumerable<Player>? players) =>
        players == null ? new List<Player>() : players.OrderBy(p => p.NetId).ToList();

    /// <summary>
    /// Alive pets whose <see cref="Creature.Monster"/> is <see cref="DuelMonsterModel"/>, from a
    /// <see cref="PetsSnapshotOrderedByCombatId"/> pass (no live <c>.Where</c> on <c>Pets</c>).
    /// </summary>
    public static List<Creature> PetsSnapshotAliveDuelMonstersOrderedByCombatId(PlayerCombatState? pcs)
    {
        var list = new List<Creature>();
        if (pcs == null)
            return list;
        foreach (Creature p in PetsSnapshotOrderedByCombatId(pcs))
        {
            if (p.IsAlive && p.Monster is DuelMonsterModel)
                list.Add(p);
        }

        return list;
    }

    /// <summary>
    /// Tribute / grid material lists: not live <see cref="PlayerCombatState.Pets"/>, but use the same ordering before kills or sync-sensitive work.
    /// </summary>
    public static List<Creature> CreatureListOrderedByCombatId(IEnumerable<Creature>? creatures) =>
        creatures == null ? new List<Creature>() : creatures.OrderBy(p => p.CombatId).ToList();

    /// <summary>
    /// When several pets match (same source binding, same stance, etc.), pick the lowest <see cref="Creature.CombatId"/>.
    /// </summary>
    public static Creature? FirstPetWhere(PlayerCombatState pcs, Func<Creature, bool> predicate)
    {
        foreach (Creature p in PetsSnapshotOrderedByCombatId(pcs))
        {
            if (predicate(p))
                return p;
        }

        return null;
    }

    /// <summary>
    /// Existence check over a <see cref="PetsSnapshotOrderedByCombatId"/> view: avoids iterating live
    /// <see cref="PlayerCombatState.Pets"/> during hooks or UI reads (collection-modified + MP ordering).
    /// </summary>
    public static bool PetsAny(PlayerCombatState? pcs, Func<Creature, bool> predicate)
    {
        if (pcs == null)
            return false;
        foreach (Creature p in PetsSnapshotOrderedByCombatId(pcs))
        {
            if (predicate(p))
                return true;
        }

        return false;
    }

    public static Creature? FindPetByCombatId(PlayerCombatState? pcs, uint combatId) =>
        pcs == null ? null : FirstPetWhere(pcs, p => p.CombatId == combatId);

    /// <summary>
    /// Stable lookup for any creature list keyed by <see cref="Creature.CombatId"/> (players/enemies/pets).
    /// </summary>
    public static Creature? FindCreatureByCombatId(IEnumerable<Creature>? creatures, uint combatId)
    {
        foreach (Creature c in CreatureListOrderedByCombatId(creatures))
        {
            if (c.CombatId == combatId)
                return c;
        }

        return null;
    }

    /// <summary>
    /// Stable snapshot for enemy fan-out loops in combat effects (damage/debuff/all-target passes).
    /// </summary>
    public static List<Creature> HittableEnemiesAliveOrderedByCombatId(CombatState? cs) =>
        cs == null
            ? new List<Creature>()
            : cs.HittableEnemies.Where(e => e.IsAlive).OrderBy(e => e.CombatId).ToList();

    /// <summary>
    /// Spell/trap zone and other combat piles: stable net id, then <see cref="CardId.Entry"/> for tie-break.
    /// </summary>
    public static IEnumerable<CardModel> CardsOrderedForMp(IEnumerable<CardModel> cards) =>
        cards.OrderBy(c => NetCombatCardDb.Instance.GetCardId(c)).ThenBy(c => c.Id?.Entry ?? string.Empty);

    public static List<CardModel> CardsSnapshotOrderedForMp(IEnumerable<CardModel>? cards) =>
        cards == null ? new List<CardModel>() : CardsOrderedForMp(cards).ToList();

    public static CardModel? FirstCardWhereStable(IEnumerable<CardModel> cards, Func<CardModel, bool> predicate) =>
        CardsOrderedForMp(cards).FirstOrDefault(predicate);

    public static CardModel? FirstCardWithNetId(IEnumerable<CardModel> cards, uint netCombatCardId) =>
        CardsOrderedForMp(cards).FirstOrDefault(c => NetCombatCardDb.Instance.GetCardId(c) == netCombatCardId);
}
