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
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Tailor_of_the_Fickle : BaseSpellCard, IYgoPlayCardActionPreSpendResourceFlow
{
    public Tailor_of_the_Fickle()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && GetReassignableEquips(Owner).Count > 0;

    internal static List<BaseMonsterCard> GetAlternateValidTargets(BaseEquipSpellCard equip, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        var current = YgoEquipSpellRegistry.GetEquippedMonster(equip);
        if (current == null)
            return new List<BaseMonsterCard>();

        return DuelMonsterFieldRegistry.GetFieldMonsters(player)
            .OfType<BaseMonsterCard>()
            .Where(m => !ReferenceEquals(m, current) && YgoEquipSpellTargetRules.IsLegalEquipTarget(equip, m))
            .ToList();
    }

    internal static List<BaseEquipSpellCard> GetReassignableEquips(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        var zonePile = YgoPlayerPiles.SpellTrapZone(player);
        if (zonePile == null)
            return new List<BaseEquipSpellCard>();

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(zonePile.Cards)
            .OfType<BaseEquipSpellCard>()
            .Where(eq => YgoEquipSpellRegistry.GetEquippedMonster(eq) != null)
            .Where(eq => GetAlternateValidTargets(eq, player).Count > 0)
            .ToList();
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!TailorOfTheFicklePlayPayload.TryTakePending(this, out var pending) || pending == null)
            return;
        if (Owner == null)
            return;
        if (!GetReassignableEquips(Owner).Contains(pending.Equip))
            return;
        if (!GetAlternateValidTargets(pending.Equip, Owner).Contains(pending.Target))
            return;

        YgoEquipSpellRegistry.Attach(pending.Equip, pending.Target);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(Owner);
        if (IsUpgraded)
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }

    public override bool TryGetPlayCardQueueOnActionEnqueuedDeferral(out string? reason)
    {
        reason = "tailor_of_the_fickle";
        return true;
    }

    async Task<bool> IYgoPlayCardActionPreSpendResourceFlow.TryPreparePreSpendPlayAsync(
        PlayCardAction action,
        Player player,
        CardModel self)
    {
        var equipPrefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        BaseEquipSpellCard? selectedEquip = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            equipPrefs,
            () => GetReassignableEquips(player));
        if (selectedEquip == null)
            return false;

        var targetPrefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        BaseMonsterCard? selectedTarget = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            targetPrefs,
            () => GetAlternateValidTargets(selectedEquip, player));
        if (selectedTarget == null)
            return false;

        TailorOfTheFicklePlayPayload.SetPending(self, new TailorOfTheFicklePendingResolution(selectedEquip, selectedTarget));
        return true;
    }

    void IYgoPlayCardActionPreSpendResourceFlow.ClearPreSpendPlayState(CardModel self) =>
        TailorOfTheFicklePlayPayload.ClearForCard(self);
}
