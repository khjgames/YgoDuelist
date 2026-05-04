using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Ancient_Chant : BaseSpellCard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    public Ancient_Chant()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override bool SupportsGraveEffectDisplayForm => true;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.God;

    public override Type[] RelatedCards => new[] { typeof(Ancient_Chant), typeof(The_Winged_Dragon_of_Ra) };

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

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        The_Winged_Dragon_of_Ra toHand = ra[0];

        await CardPileCmd.Add(
            new CardModel[] { toHand },
            hand,
            CardPilePosition.Top,
            toHand,
            false);

        await PlayerCmd.GainStars(1, player);
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override async Task OnTributeSummonedMonster(
        PlayerChoiceContext choiceContext,
        Player player,
        BaseMonsterCard summonedMonster,
        IReadOnlyList<BaseMonsterCard> tributeMonsters)
    {
        if (!ReferenceEquals(Owner, player))
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;

        if (summonedMonster is not The_Winged_Dragon_of_Ra ra)
            return;

        CardPile? banished = YgoPlayerPiles.Banished(player);
        if (banished == null)
            return;

        int sumAtk = tributeMonsters.Sum(m => m.BaseAtk);
        int sumDef = tributeMonsters.Sum(m => m.BaseDef);

        await CardPileCmd.Add(
            new CardModel[] { this },
            banished,
            CardPilePosition.Top,
            this,
            false);

        ra.ApplyTributeSummonPrintedStats(sumAtk, sumDef);
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
        foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player)))
        {
            if (c is The_Winged_Dragon_of_Ra ra)
                list.Add(ra);
        }
    }
}