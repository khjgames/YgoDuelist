using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// Searches the draw pile for Polymerization and adds it to the hand (fusion support, not a Fusion Summon).
/// </summary>
public sealed class Fusion_Sage : BaseSpellCard
{
    public Fusion_Sage()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Fusion | YgoCardPackTags.Spell | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Fusion_Sage), typeof(Polymerization) };

    protected override bool IsPlayable =>
        base.IsPlayable && Owner != null && GetPolymersInDrawPile(Owner).Count > 0;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        List<Polymerization> candidates = GetPolymersInDrawPile(player);
        if (candidates.Count == 0)
            return;

        Polymerization toHand = candidates[0];
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { toHand },
            hand,
            CardPilePosition.Top,
            toHand,
            false);
    }

    private static List<Polymerization> GetPolymersInDrawPile(Player player)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        if (draw == null)
            return new List<Polymerization>();

        return draw.Cards.OfType<Polymerization>().ToList();
    }

    protected override void OnUpgrade()
    {
    }
}
