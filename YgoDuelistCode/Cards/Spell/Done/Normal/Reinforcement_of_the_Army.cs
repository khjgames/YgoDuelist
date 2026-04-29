using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

/// <summary>
/// Reinforcement of the Army — add 1 Level 4 or lower Warrior monster from your draw pile to your hand.
/// </summary>
public sealed class Reinforcement_of_the_Army : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Reinforcement_of_the_Army()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(Reinforcement_of_the_Army) };

    protected override bool IsPlayable =>
        base.IsPlayable && Owner != null && BuildEligibleWarriors(Owner).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<CardModel> candidates = BuildEligibleWarriors(player);
        if (candidates.Count == 0)
            return false;

        return await YgoPrePlayGridSelection.TryPrepareSingleCardPayloadAsync<BaseMonsterCard>(
            player,
            sourceCard,
            candidates,
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: () => BuildEligibleWarriors(player));
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseMonsterCard chosen)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null || !draw.Cards.Contains(chosen))
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

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

    private static List<CardModel> BuildEligibleWarriors(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return new List<CardModel>();

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Warrior && m.DuelMonsterLevel <= 4)
            .Cast<CardModel>()
            .ToList();
    }
}
