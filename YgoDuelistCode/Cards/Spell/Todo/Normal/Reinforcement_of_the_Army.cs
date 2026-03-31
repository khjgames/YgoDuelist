using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// Reinforcement of the Army — add 1 Level 4 or lower Warrior monster from your draw pile to your hand.
/// </summary>
public sealed class Reinforcement_of_the_Army : BaseSpellCard
{
    public Reinforcement_of_the_Army()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Reinforcement_of_the_Army) };

    protected override bool IsPlayable =>
        base.IsPlayable && Owner != null && GetEligibleWarriorsInDraw(Owner).Any();

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        var candidates = GetEligibleWarriorsInDraw(player).ToList();
        if (candidates.Count == 0)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return;

        BaseMonsterCard chosen;
        if (candidates.Count == 1)
        {
            chosen = candidates[0];
        }
        else
        {
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs);
            var first = selected.FirstOrDefault();
            if (first is not BaseMonsterCard picked)
                return;
            chosen = picked;
        }

        await CardPileCmd.Add(
            new CardModel[] { chosen },
            hand,
            CardPilePosition.Top,
            chosen,
            false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static IEnumerable<BaseMonsterCard> GetEligibleWarriorsInDraw(Player player)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        if (draw == null)
            return Enumerable.Empty<BaseMonsterCard>();

        return draw.Cards
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Warrior && m.DuelMonsterLevel <= 4);
    }
}
