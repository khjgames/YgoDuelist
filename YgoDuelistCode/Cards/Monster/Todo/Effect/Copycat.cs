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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Copycat : EffectMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "COPYCAT-COIN";

    private int _intentMirrorBonusThisTurn;

    public Copycat()
        : base(
            cost: 2,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Chance | YgoCardPackTags.Light | YgoCardPackTags.Spellcaster | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    public override Type[] BundledCards => new[] { typeof(Copycat) };

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Copycat),
        typeof(Heads),
        typeof(Tails)
    };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new DynamicVar[] { new IntVar("Cap", 35) });

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-COPYCAT.activated_effect.description";

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (!ReferenceEquals(target, this) || _intentMirrorBonusThisTurn <= 0)
            return StatEffectTotal.None;

        int b = _intentMirrorBonusThisTurn;
        return new StatEffectTotal(b, b);
    }

    /// <summary>Clears the intent-mirror ATK/DEF bonus at end of the player's turn.</summary>
    public void ClearIntentMirrorBonusForTurnEnd() => _intentMirrorBonusThisTurn = 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? playerCreature = player?.Creature;
        if (playerCreature?.CombatState is not CombatState cs)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
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
        ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(playerCreature, pet, cardPlay);
        bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, CoinSalt, mix);

        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateCoinFlipResultCard(cs, player, flipIsHeads);
        var coinPrompt = new LocString("cards", "YGODUELIST-COPYCAT.coin_result.selection");
        var coinPrefs = new CardSelectorPrefs(coinPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, new List<CardModel> { resultCard }, player, coinPrefs);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);

        bool win = calledHeads == flipIsHeads;
        if (!win)
        {
            _intentMirrorBonusThisTurn = 0;
            await CreatureCmd.Damage(
                choiceContext,
                playerCreature,
                5m,
                ValueProp.Unblockable | ValueProp.Unpowered,
                playerCreature,
                source);
            return;
        }

        int cap = (int)source.DynamicVars["Cap"].BaseValue;
        int maxIntent = 0;
        foreach (Creature enemy in cs.HittableEnemies)
        {
            if (enemy.IsAlive)
                maxIntent = Math.Max(maxIntent, YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, playerCreature));
        }

        _intentMirrorBonusThisTurn = Math.Min(maxIntent, cap);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Cap"].BaseValue = 50m;
    }
}
