using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Fairy_of_the_Spring : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Fairy_of_the_Spring()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && GetEquipSpellsInGraveyard(Owner).Any();

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        var equips = GetEquipSpellsInGraveyard(player).ToList();
        if (equips.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                equips,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        CardModel? chosen = selected.FirstOrDefault();
        if (chosen is not BaseEquipSpellCard)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseEquipSpellCard equip)
            return;

        if (!GraveyardRelic.GetGraveyardCards(Owner).Contains(equip))
            return;

        CardPile? hand = PileType.Hand.GetPile(Owner);
        if (hand == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { equip },
            hand,
            CardPilePosition.Top,
            equip,
            false);

        if (!IsUpgraded)
            FairyOfSpringReturnedEquipLock.Mark(equip);
    }

    protected override void OnUpgrade()
    {
    }

    private static IEnumerable<BaseEquipSpellCard> GetEquipSpellsInGraveyard(Player player)
    {
        return GraveyardRelic.GetGraveyardCards(player).OfType<BaseEquipSpellCard>();
    }
}
