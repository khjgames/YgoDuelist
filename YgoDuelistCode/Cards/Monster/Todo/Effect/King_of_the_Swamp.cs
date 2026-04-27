using YgoDuelist.YgoDuelistCode.Cards;
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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class King_of_the_Swamp : EffectMonsterCard, IFusionMaterialSubstitute
{
    private static readonly LocString SearchPrompt =
        new LocString("combat_messages", "KING_OF_THE_SWAMP_HAND_EFFECT_SELECT");

    public King_of_the_Swamp()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 5,
            baseDef: 11,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Fusion;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(King_of_the_Swamp),
        typeof(Polymerization),
    };

    protected override bool SupportsHandEffectForm => true;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    public override bool CanSummonDuelMonster => !IsHandEffectFormActive;

    protected override PileType GetResultPileType()
    {
        if (IsHandEffectFormActive)
            return GraveyardPile.CustomType;

        return base.GetResultPileType();
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            if (!IsHandEffectFormActive || Owner == null)
                return true;

            return BuildSearchPool(Owner).Count >= 1;
        }
    }

    private static IEnumerable<CardModel> EnumerateDrawAndDiscard(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? discard = YgoPlayerPiles.Discard(player);

        if (draw != null)
        {
            foreach (CardModel c in draw.Cards)
                yield return c;
        }

        if (discard != null)
        {
            foreach (CardModel c in discard.Cards)
                yield return c;
        }
    }

    private static List<Polymerization> BuildSearchPool(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(EnumerateDrawAndDiscard(player))
            .OfType<Polymerization>()
            .ToList();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            if (Owner?.Creature != null)
                await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            await ResolveHandEffectSearchAsync();
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    private async Task ResolveHandEffectSearchAsync()
    {
        Player? player = Owner;
        if (player == null)
            return;

        var ctx = YgoChoiceContexts.Blocking();
        var prefs = new CardSelectorPrefs(SearchPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        Polymerization? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            prefs,
            () => BuildSearchPool(player));

        if (chosen == null)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { chosen },
            hand,
            CardPilePosition.Top,
            chosen,
            false);
    }
}