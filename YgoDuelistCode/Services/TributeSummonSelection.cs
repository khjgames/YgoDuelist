using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class TributeSummonSelection
{
    private static readonly LocString TributePrompt = new LocString("combat_messages", "TRIBUTE_SUMMON_SELECT");

    /// <summary>Living duel monsters on the field with a registered source card (tribute candidates).</summary>
    public static int CountTributableFieldMonsters(Player? player)
    {
        if (player?.PlayerCombatState == null)
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

    /// <summary>Source cards for each tributable field monster, in pet iteration order.</summary>
    public static List<BaseMonsterCard> BuildTributeCandidateCards(Player player)
    {
        var list = new List<BaseMonsterCard>();
        if (player.PlayerCombatState == null)
            return list;

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            BaseMonsterCard? c = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (c != null)
                list.Add(c);
        }

        return list;
    }

    public static Creature? ResolvePetForFieldCard(Player player, BaseMonsterCard fieldSourceCard)
    {
        if (player.PlayerCombatState == null)
            return null;

        foreach (Creature p in player.PlayerCombatState.Pets)
        {
            if (p.Monster is DuelMonsterModel && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), fieldSourceCard))
                return p;
        }

        return null;
    }

    /// <summary>
    /// Opens the simple grid to pick exactly <paramref name="tributeCount"/> field monsters.
    /// Returns null if the player cancelled or the task was canceled.
    /// </summary>
    public static async Task<List<Creature>?> SelectTributesAsync(Player player, int tributeCount)
    {
        if (tributeCount <= 0)
            return new List<Creature>();

        var candidates = BuildTributeCandidateCards(player);
        if (candidates.Count < tributeCount)
            return null;

        var prefs = new CardSelectorPrefs(TributePrompt, tributeCount, tributeCount)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        var picked = selected.OfType<BaseMonsterCard>().ToList();
        if (picked.Count != tributeCount)
            return null;

        if (picked.Distinct().Count() != tributeCount)
            return null;

        var pets = new List<Creature>(tributeCount);
        foreach (BaseMonsterCard c in picked)
        {
            Creature? pet = ResolvePetForFieldCard(player, c);
            if (pet == null || !pet.IsAlive)
                return null;
            pets.Add(pet);
        }

        if (pets.Distinct().Count() != tributeCount)
            return null;

        return pets;
    }
}
