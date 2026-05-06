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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Activate Effect (1 Energy, once per turn): call Heads or Tails, then toss a coin.
/// On a correct call, you may return 1 card from your Graveyard to the top of your draw pile.
/// </summary>
public sealed class Mystical_Knight_of_Jackal : EffectMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "MYSTICAL_KNIGHT_OF_JACKAL-COIN";

    private static readonly LocString ReturnPrompt = new("cards", "YGODUELIST-MYSTICAL_KNIGHT_OF_JACKAL.return_to_draw");

    public Mystical_Knight_of_Jackal()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 27,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Light | YgoCardPackTags.Warrior | YgoCardPackTags.Chance;

    public override Type[] RelatedCards => new[] { typeof(Mystical_Knight_of_Jackal), typeof(Heads), typeof(Tails) };

    public int ActivatedEffectEnergyCost => 1;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-MYSTICAL_KNIGHT_OF_JACKAL.activated_effect.description";

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
            return BuildGraveyardCandidates(Owner).Count > 0;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Mystical_Knight_of_Jackal || Owner?.Creature?.CombatState is not CombatState cs)
            return;

        Player player = Owner;
        if (player.PlayerCombatState == null || player.Creature == null)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || !selfPet.IsAlive)
            return;

        List<CardModel> candidates = BuildGraveyardCandidates(player);
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
        var coinPrompt = new LocString("cards", "YGODUELIST-MYSTICAL_KNIGHT_OF_JACKAL.coin_result.selection");
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new List<CardModel> { resultCard }, player, coinPrompt);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);

        bool success = calledHeads == flipIsHeads;
        if (!success)
            return;

        BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        CardModel? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(ReturnPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildGraveyardCandidates(player));
        if (chosen == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }

    private static List<CardModel> BuildGraveyardCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player)).ToList();
}
