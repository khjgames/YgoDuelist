using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per-player ordered list of field monster cards marked as tribute materials this turn.
/// First N valid entries are consumed when a tribute summon resolves.
/// </summary>
public static class TributeMaterialMarkTracker
{
    private static readonly Dictionary<Player, List<BaseMonsterCard>> _marksByPlayer = new();

    public static bool ShouldShowToggleTributeSacrificeCommand(Player player)
    {
        if (player?.PlayerCombatState == null)
            return false;

        var hand = player.PlayerCombatState.Hand;
        if (hand == null)
            return false;

        int maxTributeNeeded = 0;
        bool anyAffordableTributeInHand = false;

        foreach (CardModel c in hand.Cards)
        {
            if (c is not AbstractMonsterCard am || !am.CanSummonDuelMonster)
                continue;
            int tr = am.TributeReleaseCount;
            if (tr <= 0)
                continue;

            if (!player.PlayerCombatState.HasEnoughResourcesFor(c, out _))
                continue;

            anyAffordableTributeInHand = true;
            if (tr > maxTributeNeeded)
                maxTributeNeeded = tr;
        }

        if (!anyAffordableTributeInHand || maxTributeNeeded <= 0)
            return false;

        return CountTributableFieldMonsters(player) >= maxTributeNeeded;
    }

    private static int CountTributableFieldMonsters(Player player)
    {
        if (player.PlayerCombatState == null)
            return 0;

        int n = 0;
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) != null)
                n++;
        }

        return n;
    }

    public static bool IsMarked(Player player, BaseMonsterCard card)
    {
        if (!_marksByPlayer.TryGetValue(player, out var list))
            return false;
        return list.Any(c => ReferenceEquals(c, card));
    }

    public static bool CanSatisfyTribute(Player? player, int required)
    {
        if (player == null || required <= 0)
            return true;

        PruneObsoleteMarks(player);
        if (!_marksByPlayer.TryGetValue(player, out var list))
            return false;

        int found = 0;
        foreach (BaseMonsterCard card in list)
        {
            if (IsLiveFieldMaterial(player, card))
                found++;
            if (found >= required)
                return true;
        }

        return false;
    }

    public static void PruneObsoleteMarks(Player player)
    {
        if (!_marksByPlayer.TryGetValue(player, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (!IsLiveFieldMaterial(player, list[i]))
                list.RemoveAt(i);
        }

        if (list.Count == 0)
            _marksByPlayer.Remove(player);
    }

    private static bool IsLiveFieldMaterial(Player player, BaseMonsterCard card)
    {
        Creature? pet = FindPetForMaterial(player, card);
        return pet != null && pet.IsAlive;
    }

    public static Creature? FindPetForMaterial(Player player, BaseMonsterCard source)
    {
        if (player.PlayerCombatState == null)
            return null;

        foreach (Creature p in player.PlayerCombatState.Pets)
        {
            if (p.Monster is DuelMonsterModel && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), source))
                return p;
        }

        return null;
    }

    public static async Task ToggleMarkAsync(Player player, BaseMonsterCard materialCard, Creature applier, CardModel sourceCard)
    {
        if (!_marksByPlayer.TryGetValue(player, out var list))
        {
            list = new List<BaseMonsterCard>();
            _marksByPlayer[player] = list;
        }

        int idx = -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], materialCard))
            {
                idx = i;
                break;
            }
        }

        Creature? pet = FindPetForMaterial(player, materialCard);
        if (idx >= 0)
        {
            list.RemoveAt(idx);
            if (pet != null)
                await PowerCmd.Remove<MarkedSacrificePower>(pet);
        }
        else
        {
            list.Add(materialCard);
            if (pet != null)
                await PowerCmd.Apply<MarkedSacrificePower>(pet, 1m, applier, sourceCard);
        }

        if (list.Count == 0)
            _marksByPlayer.Remove(player);
    }

    /// <summary>Removes a field card from the mark list (e.g. pet died). Does not await power removal.</summary>
    public static void RemoveCardFromMarkList(Player? player, BaseMonsterCard? card)
    {
        if (player == null || card == null)
            return;
        if (!_marksByPlayer.TryGetValue(player, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(list[i], card))
                list.RemoveAt(i);
        }

        if (list.Count == 0)
            _marksByPlayer.Remove(player);
    }

    /// <summary>
    /// Takes the first <paramref name="count"/> valid marked materials in order. If that many cannot be resolved,
    /// the list is unchanged and an empty collection is returned.
    /// </summary>
    public static async Task<IReadOnlyList<Creature>> ConsumeTributeMaterialsAsync(Player player, int count)
    {
        if (count <= 0 || player.PlayerCombatState == null)
            return Array.Empty<Creature>();

        PruneObsoleteMarks(player);
        if (!_marksByPlayer.TryGetValue(player, out var list))
            return Array.Empty<Creature>();

        var selectedIndices = new List<int>();
        var pets = new List<Creature>();
        for (int i = 0; i < list.Count && pets.Count < count; i++)
        {
            Creature? pet = FindPetForMaterial(player, list[i]);
            if (pet == null || !pet.IsAlive)
                continue;
            selectedIndices.Add(i);
            pets.Add(pet);
        }

        if (pets.Count < count)
            return Array.Empty<Creature>();

        for (int j = selectedIndices.Count - 1; j >= 0; j--)
            list.RemoveAt(selectedIndices[j]);

        foreach (Creature pet in pets)
            await PowerCmd.Remove<MarkedSacrificePower>(pet);

        if (list.Count == 0)
            _marksByPlayer.Remove(player);

        return pets;
    }

    public static async Task ClearForPlayerAsync(Player player)
    {
        if (!_marksByPlayer.TryGetValue(player, out var list))
            return;

        List<BaseMonsterCard> copy = list.ToList();
        list.Clear();
        _marksByPlayer.Remove(player);

        foreach (BaseMonsterCard card in copy)
        {
            Creature? pet = FindPetForMaterial(player, card);
            if (pet != null)
                await PowerCmd.Remove<MarkedSacrificePower>(pet);
        }
    }

    public static void ClearAll()
    {
        _marksByPlayer.Clear();
    }
}
