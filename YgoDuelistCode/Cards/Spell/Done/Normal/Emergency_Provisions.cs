using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Emergency_Provisions : BaseSpellCard, IYgoPlayCardActionPreSpendResourceFlow
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Emergency_Provisions()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Heal | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Emergency_Provisions),
    };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && BuildSpellTrapZoneCandidates(Owner).Count > 0;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature == null)
            return;

        if (!EmergencyProvisionsPlayPayload.TryTakePending(this, out var selectedCards) || selectedCards == null)
            return;

        List<CardModel> toDestroy = selectedCards
            .Where(c => c.Pile?.Type == SpellTrapZonePile.CustomType && YgoSpellTrapZoneBridge.IsSpellOrTrapCard(c))
            .Distinct()
            .ToList();

        if (toDestroy.Count == 0)
            return;

        CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
        if (graveyard == null)
            return;

        bool anyField = toDestroy.Any(YgoSpellTrapZoneBridge.IsFieldSpell);

        await CardPileCmd.Add(toDestroy, graveyard, CardPilePosition.Top, this, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        if (anyField)
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);

        await CreatureCmd.Heal(player.Creature, toDestroy.Count * DynamicVars["Mgc"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(1m);
    }

    public override bool TryGetPlayCardQueueOnActionEnqueuedDeferral(out string? reason)
    {
        reason = "emergency_provisions";
        return true;
    }

    async Task<bool> IYgoPlayCardActionPreSpendResourceFlow.TryPreparePreSpendPlayAsync(
        PlayCardAction action,
        Player player,
        CardModel self)
    {
        List<CardModel> candidates = BuildSpellTrapZoneCandidates(player);
        if (candidates.Count == 0)
            return false;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, candidates.Count)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<CardModel> selected = await YgoOrderedCardSelection.TryChooseManyAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            prefs,
            () => BuildSpellTrapZoneCandidates(player));
        selected = selected
            .Where(c => c.Pile?.Type == SpellTrapZonePile.CustomType && YgoSpellTrapZoneBridge.IsSpellOrTrapCard(c))
            .Distinct()
            .ToList();

        if (selected.Count == 0)
            return false;

        EmergencyProvisionsPlayPayload.SetPending(self, selected);
        return true;
    }

    void IYgoPlayCardActionPreSpendResourceFlow.ClearPreSpendPlayState(CardModel self) =>
        EmergencyProvisionsPlayPayload.ClearForCard(self);

    private static List<CardModel> BuildSpellTrapZoneCandidates(Player player)
    {
        CardPile? zonePile = YgoPlayerPiles.SpellTrapZone(player);
        return zonePile == null
            ? []
            : YgoMpCombatOrder.CardsSnapshotOrderedForMp(zonePile.Cards)
                .Where(YgoSpellTrapZoneBridge.IsSpellOrTrapCard)
                .ToList();
    }
}
