using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Ancient_Chant : BaseSpellCard, IYgoApplyAncientChantPowerWhenBanishedFromGraveyard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    public Ancient_Chant()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.God;

    public override Type[] BundledCards => new[] { typeof(The_Winged_Dragon_of_Ra) };

    public override Type[] RelatedCards => new[] { typeof(Ancient_Chant), typeof(The_Winged_Dragon_of_Ra) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<AncientChantRaTributeBuffPower>();
        }
    }

    protected override bool IsPlayable =>
        base.IsPlayable && Owner != null && FindRaCandidates(Owner).Count > 0;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature == null)
            return;

        List<The_Winged_Dragon_of_Ra> ra = FindRaCandidates(player);
        if (ra.Count == 0)
            return;

        The_Winged_Dragon_of_Ra toHand = ra[0];
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(new CardModel[] { toHand }, hand, CardPilePosition.Top, toHand, false);
        await PlayerCmd.GainStars(1, player);
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public async Task ApplyPowerWhenBanishedFromGraveyardAsync(Player player)
    {
        if (player.Creature == null)
            return;
        await PowerCmd.Apply<AncientChantRaTributeBuffPower>(player.Creature, 1m, player.Creature, this);
    }

    private static List<The_Winged_Dragon_of_Ra> FindRaCandidates(Player player)
    {
        var list = YgoPlayerPiles.OrderedCardsOfTypeFromPiles<The_Winged_Dragon_of_Ra>(
            player,
            YgoPlayerPiles.Draw,
            YgoPlayerPiles.Discard);
        AppendRaFromGraveyard(player, list);
        return list;
    }

    private static void AppendRaFromGraveyard(Player player, List<The_Winged_Dragon_of_Ra> list)
    {
        foreach (CardModel c in YgoPlayerPiles.GraveyardCards(player))
        {
            if (c is The_Winged_Dragon_of_Ra ra)
                list.Add(ra);
        }
    }
}
