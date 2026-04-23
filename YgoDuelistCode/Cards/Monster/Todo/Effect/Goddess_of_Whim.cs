using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Activate Effect — 1 Energy: call a coin flip; right call doubles ATK this turn, wrong call halves it.</summary>
public sealed class Goddess_of_Whim : EffectMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "GODDESS_OF_WHIM-COIN";

    public Goddess_of_Whim()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 9,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Chance;

    public override Type[] RelatedCards => new[]
    {
        typeof(Goddess_of_Whim),
        typeof(Heads),
        typeof(Tails)
    };

    public int ActivatedEffectEnergyCost => 1;
    public CardType ActivatedEffectCardType => CardType.Attack;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GODDESS_OF_WHIM.activated_effect.description";

    protected override StatEffectTotalMultiplier GetSelfStatMultiplier()
    {
        if (Owner?.PlayerCombatState == null)
            return base.GetSelfStatMultiplier();

        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(p, this))
                continue;
            if (!MonsterCommandRegistry.TryGet(p, out MonsterCommandState s))
                break;
            if (s.GoddessOfWhimAtkMultiplierThisTurn == 1m)
                break;
            return new StatEffectTotalMultiplier(s.GoddessOfWhimAtkMultiplierThisTurn, 1m);
        }

        return base.GetSelfStatMultiplier();
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Goddess_of_Whim || Owner?.Creature == null)
            return;

        Player player = Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, player);
        if (pet == null || player.Creature.CombatState is not CombatState cs)
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
        ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(player.Creature, pet, cardPlay);
        bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, CoinSalt, mix);

        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateCoinFlipResultCard(cs, player, flipIsHeads);
        var coinPrompt = new LocString("cards", "YGODUELIST-GODDESS_OF_WHIM.coin_result.selection");
        var coinPrefs = new CardSelectorPrefs(coinPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new List<CardModel> { resultCard }, player, coinPrefs);

        var state = MonsterCommandRegistry.GetOrCreate(pet);
        state.GoddessOfWhimAtkMultiplierThisTurn = calledHeads == flipIsHeads ? 2m : 0.5m;
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
}
