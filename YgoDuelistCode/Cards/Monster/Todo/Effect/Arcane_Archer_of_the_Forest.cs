using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Arcane_Archer_of_the_Forest : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Arcane_Archer_of_the_Forest()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 9,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-ARCANE_ARCHER_OF_THE_FOREST.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner?.PlayerCombatState?.Pets.Any(p =>
            p.IsAlive
            && DuelMonsterFieldRegistry.GetSourceCardForPet(p) is BaseMonsterCard c
            && c.DuelMonsterAttribute == DuelMonsterAttribute.Earth
            && !ReferenceEquals(c, this)) == true;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        var player = source.Owner;
        if (player?.PlayerCombatState?.Pets == null)
            return;

        if (cardPlay.Target == null)
            return;

        Creature? sourcePet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source);
        if (sourcePet == null)
            return;

        // Tribute any 1 Earth monster on your side (excluding this monster itself).
        var candidates = new List<(Creature pet, BaseMonsterCard card)>();
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive)
                continue;

            BaseMonsterCard? card = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (card == null)
                continue;

            if (card.DuelMonsterAttribute != DuelMonsterAttribute.Earth)
                continue;

            if (ReferenceEquals(card, source))
                continue;

            candidates.Add((pet, card));
        }

        if (candidates.Count == 0)
            return;

        var candidateCards = candidates.Select(c => c.card).ToList();
        var prefs = new CardSelectorPrefs(TributePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(choiceContext, candidateCards, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        BaseMonsterCard? chosen = picked.OfType<BaseMonsterCard>().FirstOrDefault();
        if (chosen == null)
            return;

        Creature? tributePet = candidates.FirstOrDefault(c => ReferenceEquals(c.card, chosen)).pet;
        if (tributePet == null || !tributePet.IsAlive)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(sourcePet, true);

        await CreatureCmd.Kill(tributePet, force: true);

        CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
        if (graveyard != null)
            await CardPileCmd.Add(new[] { chosen }, graveyard, CardPilePosition.Top, chosen, false);

        // Apply statuses to the targeted enemy.
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, 1m, sourcePet, source);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, 3m, sourcePet, source);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
