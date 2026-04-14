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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

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

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);
        if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
            return;

        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        await TryOfferGazelleFromDeckAsync(ctx, player);
    }

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        await TryOfferGazelleFromDeckAsync(choiceContext, player);

    private static async Task TryOfferGazelleFromDeckAsync(PlayerChoiceContext choiceContext, Player player)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? hand = PileType.Hand.GetPile(player);
        if (draw == null || hand == null)
            return;

        List<Gazelle_the_King_of_Mythical_Beasts> gazelles = draw.Cards.OfType<Gazelle_the_King_of_Mythical_Beasts>().ToList();
        if (gazelles.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(AddGazellePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> pick = await CardSelectCmd.FromSimpleGrid(choiceContext, gazelles, player, prefs);
        if (pick.FirstOrDefault() is not Gazelle_the_King_of_Mythical_Beasts gazelle)
            return;

        await CardPileCmd.Add(new CardModel[] { gazelle }, hand, CardPilePosition.Top, gazelle, false);
    }
}
