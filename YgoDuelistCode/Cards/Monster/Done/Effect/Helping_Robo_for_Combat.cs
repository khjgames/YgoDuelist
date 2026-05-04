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

/// <summary>When this attacks: draw <c>Mgc</c>, then discard <c>Mgc</c> cards from your hand.</summary>
public sealed class Helping_Robo_for_Combat : EffectMonsterCard
{
    public override int AttackPortionCount => 2;
    private static readonly LocString DiscardPrompt = new("cards", "YGODUELIST-HELPING_ROBO_FOR_COMBAT.hand_select");

    public Helping_Robo_for_Combat()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 16,
            baseDef: 0,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Machine | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Helping_Robo_for_Combat) };

    protected override async Task BeforeAttackCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            Owner.PlayerCombatState,
            p => DuelMonsterFieldRegistry.HasSourceCard(p, this));
        if (pet == null || !pet.IsAlive)
            return;

        int n = (int)DynamicVars["Mgc"].BaseValue;
        if (n <= 0)
            return;

        await CardPileCmd.Draw(choiceContext, n, Owner);

        CardPile? hand = YgoPlayerPiles.Hand(Owner);
        CardPile? discard = YgoPlayerPiles.Discard(Owner);
        if (hand == null || discard == null)
            return;

        int toDiscard = Math.Min(n, hand.Cards.Count);
        if (toDiscard <= 0)
            return;

        List<CardModel> candidates = BuildDiscardCandidates(Owner, this);
        if (candidates.Count == 0)
            return;

        toDiscard = Math.Min(toDiscard, candidates.Count);

        List<CardModel> pick = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(DiscardPrompt, toDiscard, toDiscard) { Cancelable = false },
            () => BuildDiscardCandidates(Owner, this),
            maxResults: toDiscard);
        foreach (CardModel c in pick)
            await CardPileCmd.Add(new[] { c }, discard, CardPilePosition.Top, c, false);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }

    private static List<CardModel> BuildDiscardCandidates(Player player, CardModel sourceCard) =>
        TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, sourceCard);
}
