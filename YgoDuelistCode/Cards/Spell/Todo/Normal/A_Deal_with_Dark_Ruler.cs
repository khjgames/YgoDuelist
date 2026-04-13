using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// If a Level 8+ monster you controlled was sent to the Graveyard this turn: Special Summon 1 <see cref="Berserk_Dragon"/> from your hand or Deck.
/// </summary>
public sealed class A_Deal_with_Dark_Ruler : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public A_Deal_with_Dark_Ruler()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] BundledCards => new[] { typeof(Berserk_Dragon) };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && YgoDealWithDarkRulerState.HasLevel8PlusMonsterSentToGraveyardThisTurn(Owner)
        && CollectBerserkDragonCandidates(Owner).Count > 0
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<Berserk_Dragon> candidates = CollectBerserkDragonCandidates(player);
        if (candidates.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        var chosen = selected.FirstOrDefault() as Berserk_Dragon;
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not Berserk_Dragon berserk)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        CardPile? draw = PileType.Draw.GetPile(player);
        bool inHand = hand != null && hand.Cards.Contains(berserk);
        bool inDeck = draw != null && draw.Cards.Contains(berserk);
        if (!inHand && !inDeck)
            return;

        YgoDealWithDarkRulerState.EnterDealWithDarkRulerSummonBypass();
        try
        {
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, berserk, choiceContext);
        }
        finally
        {
            YgoDealWithDarkRulerState.ExitDealWithDarkRulerSummonBypass();
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static List<Berserk_Dragon> CollectBerserkDragonCandidates(Player player)
    {
        var list = new List<Berserk_Dragon>();
        Append(PileType.Hand.GetPile(player), list);
        Append(PileType.Draw.GetPile(player), list);
        return list;
    }

    private static void Append(CardPile? pile, List<Berserk_Dragon> list)
    {
        if (pile == null)
            return;
        foreach (CardModel c in pile.Cards)
        {
            if (c is Berserk_Dragon b)
                list.Add(b);
        }
    }
}
