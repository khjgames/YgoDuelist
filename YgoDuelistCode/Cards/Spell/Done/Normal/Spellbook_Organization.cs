using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Spellbook_Organization : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 0m) };

    public Spellbook_Organization()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Spellbook_Organization),
    };

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var drawPile = YgoPlayerPiles.Draw(player);
        if (drawPile == null)
            return;

        var top = drawPile.Cards.Take(3).ToList();
        if (top.Count == 0)
            return;

        var remaining = top.ToList();
        var chosenTopToBottom = new List<CardModel>(remaining.Count);

        while (remaining.Count > 0)
        {
            CardModel? pick;
            if (remaining.Count == 1)
            {
                pick = remaining[0];
            }
            else
            {
                try
                {
                    pick = await CardSelectCmd.FromChooseACardScreen(choiceContext, remaining, player, canSkip: false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            if (pick == null)
                return;

            remaining.Remove(pick);
            chosenTopToBottom.Add(pick);
        }

        // Rebuild top of draw pile: Add to top from bottom-most to top-most.
        for (int i = chosenTopToBottom.Count - 1; i >= 0; i--)
            await CardPileCmd.Add(new[] { chosenTopToBottom[i] }, drawPile, CardPilePosition.Top, this, false);

        int bonusDraw = (int)DynamicVars["Mgc"].BaseValue;
        if (bonusDraw > 0)
            await CardPileCmd.Draw(choiceContext, bonusDraw, player);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
