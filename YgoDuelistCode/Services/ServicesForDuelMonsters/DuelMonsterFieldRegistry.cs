using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Tracks which YgoDuelist monster cards have active duel monster summons for each player,
/// so we can apply their field effects (auras) when calculating stats.
/// </summary>
public static class DuelMonsterFieldRegistry
{
    private static readonly Dictionary<Player, HashSet<BaseMonsterCard>> _byPlayer = new();
    private static readonly Dictionary<Creature, BaseMonsterCard> _petToCard = new();

    public static void RegisterSummon(Player player, BaseMonsterCard card, Creature pet)
    {
        if (player == null || card == null)
            return;

        if (!_byPlayer.TryGetValue(player, out var set))
        {
            set = new HashSet<BaseMonsterCard>();
            _byPlayer[player] = set;
        }

        set.Add(card);
        if (pet != null)
            _petToCard[pet] = card;
    }

    public static IReadOnlyCollection<BaseMonsterCard> GetFieldMonsters(Player? player)
    {
        if (player == null)
            return System.Array.Empty<BaseMonsterCard>();

        return _byPlayer.TryGetValue(player, out var set)
            ? set
            : System.Array.Empty<BaseMonsterCard>();
    }

    public static BaseMonsterCard? GetSourceCardForPet(Creature pet)
    {
        return _petToCard.TryGetValue(pet, out var card) ? card : null;
    }

    /// <summary>
    /// Called when a duel monster pet dies; removes its mappings so it no longer
    /// contributes field effects or blocks a zone.
    /// </summary>
    public static void UnregisterPet(Creature pet)
    {
        if (pet == null)
            return;

        if (!_petToCard.TryGetValue(pet, out var card))
            return;

        _petToCard.Remove(pet);

        var owner = pet.PetOwner;
        if (owner != null && _byPlayer.TryGetValue(owner, out var set))
        {
            set.Remove(card);
            if (set.Count == 0)
                _byPlayer.Remove(owner);
        }
    }

    /// <summary>
    /// Clears all field/pet tracking. Call at end of combat so the next combat
    /// does not see summons from previous combats (e.g. CalcDuelMonsterStats).
    /// </summary>
    public static void ClearAll()
    {
        _byPlayer.Clear();
        _petToCard.Clear();
    }
}
