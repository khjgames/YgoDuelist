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

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Fairy_of_the_Spring : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Fairy_of_the_Spring()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override bool UseAlternateUpgradedDescription => true;

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && BuildEquipCandidates(Owner).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<CardModel> equips = BuildEquipCandidates(player);
        if (equips.Count == 0)
            return false;

        return await YgoPrePlayGridSelection.TryPrepareSingleCardPayloadAsync<BaseEquipSpellCard>(
            player,
            sourceCard,
            equips,
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: () => BuildEquipCandidates(player));
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseEquipSpellCard equip)
            return;

        if (!YgoPlayerPiles.GraveyardContains(Owner, equip))
            return;

        CardPile? hand = YgoPlayerPiles.Hand(Owner);
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

    private static List<CardModel> BuildEquipCandidates(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseEquipSpellCard>()
        .Cast<CardModel>()
        .ToList();
}
