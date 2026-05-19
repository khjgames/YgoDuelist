using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Special Summoned. When Normal Summoned or flipped face-up: on your next turn start, look at the top card of your draw pile and put it on top or bottom.
/// </summary>
public sealed class Maharaghi : SpiritEffectMonsterCard,
    IMonsterFlipEffect,
    IYgoOwnerTurnStartFieldMonsterEffect
{
    private static readonly LocString ScryBottomPrompt = new("cards", "YGODUELIST-MAHARAGHI.scry_bottom");

    private bool _pendingScryOnNextOwnerTurnStart;

    public Maharaghi()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Maharaghi) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            _ =>
            {
                _pendingScryOnNextOwnerTurnStart = true;
                return Task.CompletedTask;
            });

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        await RunOnFlipSummonedFromCommandMenuAsync(
            choiceContext,
            _ =>
            {
                _pendingScryOnNextOwnerTurnStart = true;
                return Task.CompletedTask;
            });

    public Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Maharaghi)
            return Task.CompletedTask;
        _pendingScryOnNextOwnerTurnStart = true;
        return Task.CompletedTask;
    }

    public bool IsOwnerTurnStartFieldMonsterEffectActive() =>
        Owner != null && _pendingScryOnNextOwnerTurnStart && DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, this);

    public async Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        if (!_pendingScryOnNextOwnerTurnStart || Owner == null || !ReferenceEquals(owner, Owner))
            return;
        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(owner, this))
            return;

        _pendingScryOnNextOwnerTurnStart = false;

        CardPile? draw = YgoPlayerPiles.Draw(owner);
        if (draw == null || draw.Cards.Count == 0)
            return;

        List<CardModel> top = YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards).Take(1).ToList();
        CardModel peeked = top[0];

        try
        {
            CardModel? viewed = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                new List<CardModel> { peeked },
                owner,
                canSkip: false);
            if (viewed == null)
                return;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        CardPile? hand = YgoPlayerPiles.Hand(owner);
        if (hand == null)
            return;

        await CardPileCmd.Add(peeked, hand, CardPilePosition.Top, this, false);

        var bottomPrefs = new CardSelectorPrefs(ScryBottomPrompt, 0, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        CardModel? putOnBottom = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            owner,
            bottomPrefs,
            () => new List<CardModel> { peeked });

        if (putOnBottom != null)
            await CardPileCmd.Add(peeked, draw, CardPilePosition.Bottom, this, false);
        else
            await CardPileCmd.Add(peeked, draw, CardPilePosition.Top, this, false);
    }
}
