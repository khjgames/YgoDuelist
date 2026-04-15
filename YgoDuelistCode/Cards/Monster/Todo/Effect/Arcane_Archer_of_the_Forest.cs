using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
    , IMonsterActivatedEffectPrePlaySelection
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
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Arcane_Archer_of_the_Forest),
    };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 2m) });

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-ARCANE_ARCHER_OF_THE_FOREST.activated_effect.description";
    public bool ActivatedEffectConsumesOncePerTurnSlot => false;

    public bool IsActivatedEffectAvailable => IsEarthTributeAvailable(Owner);

    public override bool IsActivatedEffectAvailableInCommandContext(Player? commandOwner) => IsEarthTributeAvailable(commandOwner);

    /// <summary>Another Earth duel monster on the field to tribute (not this card). Uses <paramref name="playerContext"/> when the field card's Owner is not set.</summary>
    internal bool IsEarthTributeAvailable(Player? playerContext)
    {
        Player? player = playerContext ?? Owner;
        if (player?.PlayerCombatState == null)
            return false;

        return player.PlayerCombatState.Pets.Any(p =>
            p.IsAlive
            && DuelMonsterFieldRegistry.GetSourceCardForPet(p) is BaseMonsterCard c
            && c.DuelMonsterAttribute == DuelMonsterAttribute.Earth
            && !ReferenceEquals(c, this));
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.PlayerCombatState?.Pets == null)
            return;

        if (cardPlay.Target == null)
            return;

        Creature? sourcePet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (sourcePet == null)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out var chosen) || chosen == null)
            return;

        Creature? tributePet = player.PlayerCombatState.Pets
            .FirstOrDefault(p => p.IsAlive && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), chosen));
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
        if (graveyard != null)
            await CardPileCmd.Add(new[] { chosen }, graveyard, CardPilePosition.Top, chosen, false);

        await PowerCmd.Apply<WeakPower>(cardPlay.Target, source.DynamicVars["Mgc"].BaseValue, sourcePet, source);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, source.DynamicVars["Mgc2"].BaseValue, sourcePet, source);
    }

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(MegaCrit.Sts2.Core.Entities.Players.Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState?.Pets == null)
            return false;

        var candidates = new List<BaseMonsterCard>();
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (!pet.IsAlive)
                continue;

            BaseMonsterCard? card = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (card == null || ReferenceEquals(card, source))
                continue;
            if (card.DuelMonsterAttribute != DuelMonsterAttribute.Earth)
                continue;

            candidates.Add(card);
        }

        if (candidates.Count == 0)
            return false;

        var prefs = new CardSelectorPrefs(TributePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        BaseMonsterCard? chosen = picked.OfType<BaseMonsterCard>().FirstOrDefault();
        if (chosen == null)
            return false;

        ActivatedEffectTributeSelectionPayload.SetPending(source, chosen);
        return true;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
        DynamicVars["Mgc2"].BaseValue = 4m;
    }
}
