using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Patches;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>FLIP helpers for returning or destroying another duel monster you control.</summary>
public static class YgoFlipReturnOwnFieldMonsterToHand
{
    public static async Task TryReturnToHandAsync(Player owner, NormalMonsterCard targetFieldMonster)
    {
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(targetFieldMonster, owner);
        if (pet == null || targetFieldMonster is not BaseMonsterCard bm)
            return;
        await DuelMonsterPetDeathPatch.ReleaseLiveFieldMonsterToHandAsync(owner, pet, bm);
    }

    public static async Task RunFlipReturnOneOtherControlledWithConduitAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        AbstractMonsterCard flipper,
        LocString selectPrompt,
        bool upgradedGivesEnergy)
    {
        if (owner.PlayerCombatState == null)
            return;

        List<NormalMonsterCard> candidates = BuildOtherControlledFieldMonsters(owner, flipper);
        if (candidates.Count == 0)
            return;

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates.Cast<CardModel>().ToList(),
                owner,
                new CardSelectorPrefs(selectPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true
                });
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (pick.FirstOrDefault() is not NormalMonsterCard target)
            return;

        await TryReturnToHandAsync(owner, target);
        if (owner.Creature == null)
            return;

        await PlayerCmd.GainStars(1, owner);
        if (upgradedGivesEnergy)
            await PlayerCmd.GainEnergy(1, owner);
    }

    public static async Task RunFlipDestroyOneOtherControlledWithConduitAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        AbstractMonsterCard flipper,
        LocString selectPrompt,
        bool upgradedGivesEnergy)
    {
        if (owner.PlayerCombatState == null)
            return;

        List<NormalMonsterCard> candidates = BuildOtherControlledFieldMonsters(owner, flipper);
        if (candidates.Count == 0)
            return;

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates.Cast<CardModel>().ToList(),
                owner,
                new CardSelectorPrefs(selectPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true
                });
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (pick.FirstOrDefault() is not NormalMonsterCard target)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(target, owner);
        if (pet == null)
            return;

        await CreatureCmd.Kill(pet, force: true);
        if (owner.Creature == null)
            return;

        await PlayerCmd.GainStars(1, owner);
        if (upgradedGivesEnergy)
            await PlayerCmd.GainEnergy(1, owner);
    }

    private static List<NormalMonsterCard> BuildOtherControlledFieldMonsters(Player owner, AbstractMonsterCard flipper)
    {
        var list = new List<NormalMonsterCard>();
        foreach (Creature p in owner.PlayerCombatState!.Pets)
        {
            if (!p.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(p) is not NormalMonsterCard nm)
                continue;
            if (ReferenceEquals(nm, flipper))
                continue;
            list.Add(nm);
        }

        return list;
    }
}
