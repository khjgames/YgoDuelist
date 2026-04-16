using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Raigeki_Break : BaseTrapCard, IYgoPrePlayCancelableGridSelection
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 10m) };

    public Raigeki_Break()
        : base(cost: 1, cardType: CardType.Attack, rarity: CardRarity.Uncommon, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Trap;

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Raigeki_Break),
    };

    public override bool CardShowsBlightKeyword => true;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && GetDestroyableHandCards(Owner, this).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        var candidates = GetDestroyableHandCards(player, sourceCard);
        GD.Print(
            $"[YgoDuelist][MP][RaigekiBreak] preplay_begin owner={player.NetId} source={sourceCard.Id?.Entry} candidates={candidates.Count}");
        if (candidates.Count == 0)
        {
            GD.Print(
                $"[YgoDuelist][MP][RaigekiBreak] preplay_cancel_no_candidates owner={player.NetId} source={sourceCard.Id?.Entry}");
            return false;
        }

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            GD.Print(
                $"[YgoDuelist][MP][RaigekiBreak] preplay_cancel_user owner={player.NetId} source={sourceCard.Id?.Entry}");
            return false;
        }

        var chosen = selected.FirstOrDefault();
        if (chosen == null || !candidates.Contains(chosen))
        {
            GD.Print(
                $"[YgoDuelist][MP][RaigekiBreak] preplay_cancel_invalid_choice owner={player.NetId} source={sourceCard.Id?.Entry}");
            return false;
        }

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        GD.Print(
            $"[YgoDuelist][MP][RaigekiBreak] preplay_confirm owner={player.NetId} source={sourceCard.Id?.Entry} discard={chosen.Id?.Entry}");
        return true;
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        var player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? chosen) || chosen == null)
        {
            GD.Print(
                $"[YgoDuelist][MP][RaigekiBreak] onplay_missing_payload owner={player.NetId} source={Id?.Entry}");
            return;
        }

        var hand = PileType.Hand.GetPile(player);
        if (hand == null || !hand.Cards.Contains(chosen))
        {
            GD.Print(
                $"[YgoDuelist][MP][RaigekiBreak] onplay_chosen_not_in_hand owner={player.NetId} source={Id?.Entry} discard={chosen.Id?.Entry}");
            return;
        }

        GD.Print(
            $"[YgoDuelist][MP][RaigekiBreak] onplay_send_to_grave owner={player.NetId} source={Id?.Entry} discard={chosen.Id?.Entry}");
        await SendHandCardToGraveyard(choiceContext, player, chosen);

        await PowerCmd.Apply<BlightPower>(cardPlay.Target, DynamicVars["Mgc"].BaseValue, base.Owner.Creature, this);
        GD.Print(
            $"[YgoDuelist][MP][RaigekiBreak] onplay_apply_blight owner={player.NetId} source={Id?.Entry} target={cardPlay.Target.ModelId}");
    }

    private static List<CardModel> GetDestroyableHandCards(Player player, CardModel sourceCard)
    {
        var hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return new List<CardModel>();

        return hand.Cards.Where(c => c != sourceCard).ToList();
    }

    private static async Task SendHandCardToGraveyard(PlayerChoiceContext choiceContext, Player player, CardModel card)
    {
        var graveyardPile = GraveyardPile.CustomType.GetPile(player);
        if (graveyardPile == null)
            return;

        await CardPileCmd.Add(
            new[] { card },
            graveyardPile,
            CardPilePosition.Top,
            card,
            false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(4m);
    }
}
