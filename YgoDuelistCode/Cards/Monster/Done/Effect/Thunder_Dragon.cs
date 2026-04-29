using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YgoDuelist.YgoDuelistCode.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Thunder_Dragon : EffectMonsterCard
{
    private static readonly LocString SearchPrompt =
        new LocString("combat_messages", "THUNDER_DRAGON_HAND_EFFECT_SELECT");

    public Thunder_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 16,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Thunder)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light;

    public override bool BundleGrantsExtraCopyOfSelf => true;

    public override Type[] BundledCards => new[] { typeof(Thunder_Dragon) };

    protected override bool SupportsHandEffectForm => true;

    /// <summary>
    /// Hand effect must spend no conduit stars: <see cref="BaseStarCost"/> is snapshotted on first read, so this
    /// overrides the value used for play checks and payment (<see cref="CardModel.GetStarCostWithModifiers"/>).
    /// </summary>
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

    private static List<Thunder_Dragon> BuildSearchPool(Player player)
    {
        var list = new List<Thunder_Dragon>();
        foreach (CardModel c in EnumerateDrawAndDiscard(player))
        {
            if (c is Thunder_Dragon td)
                list.Add(td);
        }

        return list;
    }

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

        var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
        var prefs = new CardSelectorPrefs(SearchPrompt, 1, 2)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<Thunder_Dragon> chosen = await YgoOrderedCardSelection.TryChooseManyAsync(
            ctx,
            player,
            prefs,
            () => BuildSearchPool(player),
            maxResults: 2);
        if (chosen.Count == 0)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        foreach (Thunder_Dragon c in chosen)
            await CardPileCmd.Add(new[] { c }, hand, CardPilePosition.Top, c, false);
    }
}
