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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Spellbook_Organization : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 0m) };

    public Spellbook_Organization()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var drawPile = PileType.Draw.GetPile(player);
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
