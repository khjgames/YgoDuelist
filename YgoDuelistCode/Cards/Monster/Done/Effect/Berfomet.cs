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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Berfomet : EffectMonsterCard
{
    private static readonly LocString AddGazellePrompt = new("cards", "YGODUELIST-BERFOMET.add_gazelle");

    public Berfomet()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 14,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Berfomet), typeof(Gazelle_the_King_of_Mythical_Beasts) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            ctx => TryOfferGazelleFromDeckAsync(ctx, player));

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        await RunOnFlipSummonedFromCommandMenuAsync(
            choiceContext,
            ctx => TryOfferGazelleFromDeckAsync(ctx, player));

    private static async Task TryOfferGazelleFromDeckAsync(PlayerChoiceContext choiceContext, Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (draw == null || hand == null)
            return;

        var prefs = new CardSelectorPrefs(AddGazellePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        Gazelle_the_King_of_Mythical_Beasts? gazelle = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            prefs,
            () => BuildGazelleTargets(draw));
        if (gazelle == null)
            return;

        await CardPileCmd.Add(new CardModel[] { gazelle }, hand, CardPilePosition.Top, gazelle, false);
    }

    private static List<Gazelle_the_King_of_Mythical_Beasts> BuildGazelleTargets(CardPile draw) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(draw.Cards)
        .OfType<Gazelle_the_King_of_Mythical_Beasts>()
        .ToList();
}
