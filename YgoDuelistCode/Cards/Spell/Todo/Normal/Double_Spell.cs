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

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Double_Spell : BaseSpellCard
{
    public Double_Spell()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && HasOtherSpellInHand(Owner)
        && HasEligibleReplaySpellInGraveyard(Owner);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.Creature == null)
            return;

        var prefsHand = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        CardPile? handForPick = PileType.Hand.GetPile(Owner);
        if (handForPick == null)
            return;

        List<CardModel> handCandidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(
            Owner,
            c => !ReferenceEquals(c, this) && c is BaseSpellCard,
            null);
        if (handCandidates.Count == 0)
            return;

        var handPick = await TributeSummonGridSelect.FromSimpleGrid(
            choiceContext,
            handCandidates,
            Owner,
            prefsHand,
            rebuildCanonicalForRemoteApply: () => TributeSummonGridSelect.BuildStabilizedHandCandidates(
                Owner,
                c => !ReferenceEquals(c, this) && c is BaseSpellCard,
                null),
            PlayerChoiceOptions.CancelPlayCardActions);

        if (handPick.FirstOrDefault() is not BaseSpellCard toDiscard)
            return;

        await CardCmd.Discard(choiceContext, toDiscard);

        var gyEligible = GraveyardRelic
            .GetGraveyardCards(Owner)
            .OfType<BaseSpellCard>()
            .Where(IsEligibleForReplay)
            .ToList();

        if (gyEligible.Count == 0)
            return;

        var prefsGy = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        var gyPick = await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            gyEligible,
            Owner,
            prefsGy);

        if (gyPick.OfType<BaseSpellCard>().FirstOrDefault() is not { } replay)
            return;

        if (!GraveyardRelic.GetGraveyardCards(Owner).Contains(replay))
            return;

        CardPile? hand = PileType.Hand.GetPile(Owner);
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

    private static bool HasOtherSpellInHand(Player player)
    {
        CardPile? hand = PileType.Hand.GetPile(player);
        return hand != null && hand.Cards.Any(c => c is BaseSpellCard && c is not Double_Spell);
    }

    private static bool HasEligibleReplaySpellInGraveyard(Player player)
    {
        return GraveyardRelic.GetGraveyardCards(player).OfType<BaseSpellCard>().Any(IsEligibleForReplay);
    }

    private static bool IsEligibleForReplay(BaseSpellCard s)
    {
        if (s is BaseEquipSpellCard || s is Double_Spell)
            return false;

        return s.DuelMonsterRace is DuelMonsterRace.SpellNormal or DuelMonsterRace.SpellQuickPlay;
    }
}
