using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Card_Trader : BaseContinuousSpellCard, IYgoOwnerTurnStartSpellTrapZoneEffect
{
    private const string AnnualKey = "CARD_TRADER";

    public Card_Trader()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Card_Trader) };

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public bool IsOwnerTurnStartSpellTrapZoneEffectActive() => !FaceDown;

    public async Task TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsOwnerTurnStartSpellTrapZoneEffectActive())
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null || hand.Cards.Count == 0)
            return;

        CardPile? drawPile = PileType.Draw.GetPile(player);
        if (drawPile == null)
            return;

        int maxPick = Math.Min(1, hand.Cards.Count);
        var candidates = hand.Cards.ToList();
        var prompt = new LocString("cards", "YGODUELIST-CARD_TRADER.turn_selection.title");
        prompt.Add("Max", maxPick);
        var prefs = new CardSelectorPrefs(prompt, 0, maxPick)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        IEnumerable<CardModel> picked = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, player, prefs);
        List<CardModel> toDeck = picked.Where(c => hand.Cards.Contains(c)).Distinct().Take(maxPick).ToList();
        foreach (CardModel card in toDeck)
            await CardPileCmd.Add(new[] { card }, drawPile, CardPilePosition.Random, card, false);
        if (toDeck.Count > 0)
            await CardPileCmd.Draw(choiceContext, toDeck.Count, player);
    }
}
