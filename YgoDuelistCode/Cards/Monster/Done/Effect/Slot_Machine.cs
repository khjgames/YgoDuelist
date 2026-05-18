using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Activate Effect (once per turn): call Heads or Tails, then toss a coin.
/// On a correct call, add 1 "7 Completed" from your deck or Graveyard to your hand.
/// </summary>
public sealed class Slot_Machine : EffectMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "SLOT_MACHINE-COIN";

    private static readonly LocString SearchToHandPrompt =
        new("cards", "YGODUELIST-SLOT_MACHINE.search_to_hand");

    public Slot_Machine()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 23,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Machine | YgoCardPackTags.Chance;

    public override Type[] RelatedCards => new[]
    {
        typeof(Slot_Machine),
        typeof(Card_7_Completed),
        typeof(Heads),
        typeof(Tails),
        typeof(Double_Summon),
        typeof(Mausoleum_of_the_Emperor),
        typeof(Cost_Down),
        typeof(Superheavy_Samurai_Big_Waraji),
        typeof(Chronomaly_Mayan_Machine),
        typeof(Dark_Effigy),
        typeof(Double_Coston),
    };

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-SLOT_MACHINE.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !pet.IsAlive)
                return false;
            if (!MonsterCommandRegistry.TryGet(pet, out MonsterCommandState cmd) || cmd.HasUsedActivatedEffectThisTurn)
                return false;
            return BuildCard7CompletedCandidates(Owner).Count > 0;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Slot_Machine || Owner?.Creature?.CombatState is not CombatState cs)
            return;

        Player player = Owner;
        if (player.PlayerCombatState == null || player.Creature == null)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || !selfPet.IsAlive)
            return;

        List<Card_7_Completed> candidates = BuildCard7CompletedCandidates(player);
        if (candidates.Count == 0)
            return;

        CardModel headsCall = cs.CreateCard<Heads>(player);
        CardModel tailsCall = cs.CreateCard<Tails>(player);
        var coinOptions = new List<CardModel> { headsCall, tailsCall };

        CardModel? callPick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            coinOptions,
            player,
            canSkip: false);

        if (callPick == null)
            return;

        bool calledHeads = callPick.Id.Entry == headsCall.Id.Entry;
        ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(player.Creature, selfPet, cardPlay);
        bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, CoinSalt, mix);

        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateCoinFlipResultCard(cs, player, flipIsHeads);
        var coinPrompt = new LocString("cards", "YGODUELIST-SLOT_MACHINE.coin_result.selection");
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new List<CardModel> { resultCard }, player, coinPrompt);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);

        if (calledHeads != flipIsHeads)
            return;

        Card_7_Completed? chosen;
        if (candidates.Count == 1)
        {
            chosen = candidates[0];
        }
        else
        {
            BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
            chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(SearchToHandPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true
                },
                () => BuildCard7CompletedCandidates(player)) as Card_7_Completed;
        }

        if (chosen == null)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? discard = YgoPlayerPiles.Discard(player);
        bool inDraw = draw != null && draw.Cards.Contains(chosen);
        bool inDiscard = discard != null && discard.Cards.Contains(chosen);
        bool inGraveyard = YgoPlayerPiles.GraveyardContains(player, chosen);
        if (!inDraw && !inDiscard && !inGraveyard)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<Card_7_Completed> BuildCard7CompletedCandidates(Player player)
    {
        var list = new List<Card_7_Completed>();

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards))
            {
                if (c is Card_7_Completed completed)
                    list.Add(completed);
            }
        }

        CardPile? discard = YgoPlayerPiles.Discard(player);
        if (discard != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(discard.Cards))
            {
                if (c is Card_7_Completed completed)
                    list.Add(completed);
            }
        }

        CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
        if (graveyard != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(graveyard.Cards))
            {
                if (c is Card_7_Completed completed)
                    list.Add(completed);
            }
        }

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list).OfType<Card_7_Completed>().ToList();
    }
}
