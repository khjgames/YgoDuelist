using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Double_Spell : BaseSpellCard
{
    public Double_Spell()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && BuildDiscardSpellCandidates(Owner, this).Count > 0
        && BuildReplaySpellCandidates(Owner).Count > 0;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.Creature == null)
            return;

        var prefsHand = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        CardPile? handForPick = YgoPlayerPiles.Hand(Owner);
        if (handForPick == null)
            return;

        List<CardModel> handCandidates = BuildDiscardSpellCandidates(Owner, this);
        if (handCandidates.Count == 0)
            return;

        BaseSpellCard? toDiscard = await YgoHandCardSelection.TryChooseSingleHandCardAsync<BaseSpellCard>(
            choiceContext,
            Owner,
            prefsHand,
            predicate: c => BuildDiscardSpellCandidates(Owner, this).Contains(c),
            choiceBegunOptions: PlayerChoiceOptions.CancelPlayCardActions);
        if (toDiscard == null)
            return;

        await CardCmd.Discard(choiceContext, toDiscard);

        var prefsGy = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        BaseSpellCard? replay = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            Owner,
            prefsGy,
            () => BuildReplaySpellCandidates(Owner));
        if (replay == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(Owner, replay))
            return;

        CardPile? hand = YgoPlayerPiles.Hand(Owner);
        if (hand == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { replay },
            hand,
            CardPilePosition.Top,
            replay,
            false);

        await replay.ResolveAsDoubleSpellReplayAsync(choiceContext, cardPlay);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private static List<CardModel> BuildDiscardSpellCandidates(Player player, CardModel sourceCard) =>
        TributeSummonGridSelect.BuildStabilizedHandCandidates(
            player,
            c => !ReferenceEquals(c, sourceCard) && c is BaseSpellCard,
            null);

    private static List<BaseSpellCard> BuildReplaySpellCandidates(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseSpellCard>()
        .Where(IsEligibleForReplay)
        .ToList();

    private static bool IsEligibleForReplay(BaseSpellCard s)
    {
        if (s is BaseEquipSpellCard || s is Double_Spell)
            return false;

        return s.DuelMonsterRace is DuelMonsterRace.SpellNormal or DuelMonsterRace.SpellQuickPlay;
    }
}
