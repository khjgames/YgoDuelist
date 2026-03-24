using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

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

    protected override bool SupportsHandEffectForm => true;

    /// <summary>
    /// Hand effect must spend no conduit stars: <see cref="BaseStarCost"/> is snapshotted on first read, so this
    /// overrides the value used for play checks and payment (<see cref="CardModel.GetStarCostWithModifiers"/>).
    /// </summary>
    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    public override bool CanSummonDuelMonster => !IsHandEffectFormActive;

    private int? _energyBaseBeforeHandEffectForm;

    protected override void AfterDisplayFormChanged()
    {
        base.AfterDisplayFormChanged();
        if (!IsMutable)
            return;

        if (IsHandEffectFormActive)
        {
            _energyBaseBeforeHandEffectForm ??= EnergyCost.GetWithModifiers(CostModifiers.Local);
            EnergyCost.SetCustomBaseCost(0);
        }
        else if (_energyBaseBeforeHandEffectForm is int saved)
        {
            EnergyCost.SetCustomBaseCost(saved);
            _energyBaseBeforeHandEffectForm = null;
        }
    }

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
            return CountThunderDragonsInDrawAndDiscard(Owner) >= 1;
        }
    }

    private static int CountThunderDragonsInDrawAndDiscard(Player player)
    {
        int n = 0;
        foreach (CardModel c in EnumerateDrawAndDiscard(player))
        {
            if (c is Thunder_Dragon)
                n++;
        }

        return n;
    }

    private static IEnumerable<CardModel> EnumerateDrawAndDiscard(Player player)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? discard = PileType.Discard.GetPile(player);
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

        List<Thunder_Dragon> pool = BuildSearchPool(player);
        if (pool.Count == 0)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        var prefs = new CardSelectorPrefs(SearchPrompt, 1, 2)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(ctx, pool, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        List<Thunder_Dragon> chosen = pick.OfType<Thunder_Dragon>().Distinct().ToList();
        if (chosen.Count == 0)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return;

        foreach (Thunder_Dragon c in chosen)
            await CardPileCmd.Add(new[] { c }, hand, CardPilePosition.Top, c, false);
    }
}
