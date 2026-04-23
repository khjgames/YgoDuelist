using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Ominous_Fortunetelling : BaseContinuousTrapCard, IYgoCardZoneRightClick
{
    private static readonly LocString GuessPrompt = new("combat_messages", "OMINOUS_FORTUNETELLING_GUESS_PROMPT");

    private int _activationsLeftThisTurn;
    private bool _fortuneFlowActive;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DynamicVar("Mgc", 1m) };

    public Ominous_Fortunetelling()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Ominous_Fortunetelling),
    };

    public override bool UseAlternateUpgradedDescription => true;

    
    internal bool HasFortuneActivationsRemaining => _activationsLeftThisTurn > 0;

    internal bool IsFortuneFlowActive => _fortuneFlowActive;

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        RefillFortuneActivationsForTurn();
        return Task.CompletedTask;
    }

    internal void RefillFortuneActivationsForTurn()
    {
        _activationsLeftThisTurn = IsUpgraded ? 2 : 1;
    }

    public YgoCardRightClickActivation RightClickActivationMask =>
        YgoCardRightClickActivation.SpellTrapZoneFaceUp;

    public bool TryHandleCardZoneRightClick(NHandCardHolder holder) =>
        TryHandleZoneRightClick(holder, this);

    /// <summary>Right-click on this card in the Spell/Trap zone (second hand). Returns true if the click was consumed.</summary>
    public static bool TryHandleZoneRightClick(NHandCardHolder holder, Ominous_Fortunetelling card)
    {
        if (!IsLocalControllingOwner(card))
            return false;

        if (card._fortuneFlowActive)
            return true;

        if (!card.HasFortuneActivationsRemaining)
            return false;

        _ = RunFortuneFlowAsync(holder, card);
        return true;
    }

    internal static void RefillAllInSpellTrapZoneForPlayer(Player player)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return;

        foreach (CardModel c in zone.Cards)
        {
            if (c is Ominous_Fortunetelling f)
                f.RefillFortuneActivationsForTurn();
        }
    }

    private static bool IsLocalControllingOwner(CardModel card)
    {
        if (card.Owner?.RunState == null)
            return false;
        Player? me = LocalContext.GetMe(card.Owner.RunState);
        return me != null && ReferenceEquals(me, card.Owner);
    }

    private static async Task RunFortuneFlowAsync(NHandCardHolder holder, Ominous_Fortunetelling card)
    {
        card._fortuneFlowActive = true;
        try
        {
            Player? player = card.Owner;
            if (player?.PlayerCombatState?.DrawPile is not { } draw || draw.IsEmpty)
                return;

            if (card._activationsLeftThisTurn <= 0)
                return;

            var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
            var guessChoices = new List<CardModel>
            {
                new YgoFortunetellingGuessProxyCard(YgoFortuneGuessKind.Spell),
                new YgoFortunetellingGuessProxyCard(YgoFortuneGuessKind.Trap),
                new YgoFortunetellingGuessProxyCard(YgoFortuneGuessKind.Monster),
            };

            YgoFortunetellingGuessProxyCard? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(GuessPrompt, 1, 1) { Cancelable = true },
                () => guessChoices.Cast<YgoFortunetellingGuessProxyCard>().ToList());
            YgoFortuneGuessKind? guess = picked?.GuessKind;
            if (guess == null)
                return;

            card._activationsLeftThisTurn--;

            CardModel top = draw.Cards[0];
            IReadOnlyList<CardModel> reveal = new List<CardModel> { top };
            await CardSelectCmd.FromChooseACardScreen(ctx, reveal, player, canSkip: true);

            if (GuessMatches(guess.Value, top))
            {
                int n = (int)card.DynamicVars["Mgc"].BaseValue;
                if (n > 0)
                    await CardPileCmd.Draw(ctx, n, player);
            }
        }
        finally
        {
            card._fortuneFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }

    private static bool GuessMatches(YgoFortuneGuessKind guess, CardModel top)
    {
        YgoFortuneCardCategory cat = Classify(top);
        return guess switch
        {
            YgoFortuneGuessKind.Spell => cat == YgoFortuneCardCategory.Spell,
            YgoFortuneGuessKind.Trap => cat == YgoFortuneCardCategory.Trap,
            YgoFortuneGuessKind.Monster => cat == YgoFortuneCardCategory.Monster,
            _ => false
        };
    }

    private static YgoFortuneCardCategory Classify(CardModel c)
    {
        if (c is IYgoCard y)
        {
            return y.YgoCardType switch
            {
                YgoCardType.Spell => YgoFortuneCardCategory.Spell,
                YgoCardType.Trap => YgoFortuneCardCategory.Trap,
                _ => YgoFortuneCardCategory.Monster
            };
        }

        return YgoFortuneCardCategory.Monster;
    }

    private enum YgoFortuneCardCategory
    {
        Spell,
        Trap,
        Monster
    }
}
