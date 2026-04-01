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
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Jirai_Gumo : EffectMonsterCard
{
    private const string CoinSalt = "JIRAI_GUMO-COIN";

    public Jirai_Gumo()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Insect | YgoCardPackTags.Burn | YgoCardPackTags.Chance;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    public override Type[] BundledCards => new[] { typeof(Jirai_Gumo), typeof(Second_Coin_Toss) };

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Jirai_Gumo),
        typeof(Second_Coin_Toss),
        typeof(Heads),
        typeof(Tails)
    };

    /// <summary>
    /// Field attack command: call Heads or Tails, then flip. On a wrong call, take blockable damage
    /// equal to <c>floor((current HP − 1) / 2)</c> (0 when current HP is 2 or less).
    /// </summary>
    protected override async Task BeforeAttackCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        await RunAttackDeclarationCoinIfEligibleAsync(choiceContext, Owner, Owner.Creature, this, cardPlay);
    }

    public static async Task RunAttackDeclarationCoinIfEligibleAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature playerCreature,
        Jirai_Gumo source,
        CardPlay cardPlay)
    {
        if (playerCreature.CombatState is not CombatState cs)
            return;

        Creature? pet = player.PlayerCombatState?.Pets
            .FirstOrDefault(p => DuelMonsterFieldRegistry.GetSourceCardForPet(p) == source);
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
        var coinPrompt = new LocString("cards", "YGODUELIST-JIRAI_GUMO.coin_result.selection");
        var coinPrefs = new CardSelectorPrefs(coinPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, new List<CardModel> { resultCard }, player, coinPrefs);

        if (calledHeads == flipIsHeads)
            return;

        decimal hp = playerCreature.CurrentHp;
        decimal wrongDamage = Math.Max(0m, Math.Floor((hp - 1m) / 2m));
        if (wrongDamage <= 0m)
            return;

        await CreatureCmd.Damage(choiceContext, playerCreature, wrongDamage, ValueProp.Unpowered, playerCreature, source);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
        DynamicVars["Def"].UpgradeValueBy(4m);
        if (DynamicVars.Block != null)
            DynamicVars.Block.UpgradeValueBy(4m);
    }
}
