using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Riryoku : BaseSpellCard, IYgoPlayCardActionPreSpendResourceFlow
{
    public Riryoku()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<RiryokuAtkShiftDonorPower>();
            yield return HoverTipFactory.FromPower<RiryokuAtkShiftReceiverPower>();
        }
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && BuildFieldCandidates(Owner).Count >= 2;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        if (!RiryokuPlayPayload.TryTakePending(this, out BaseMonsterCard? donor, out BaseMonsterCard? receiver)
            || donor == null || receiver == null)
            return;

        var field = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner);
        int donorAtk = (int)donor.CalcDuelMonsterStats(field).Atk;
        int newAtk = donorAtk / 2;
        int lost = donorAtk - newAtk;

        Creature? donorPet = YgoMpCombatOrder.FirstPetWhere(Owner.PlayerCombatState, p =>
            p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, donor));
        Creature? recvPet = YgoMpCombatOrder.FirstPetWhere(Owner.PlayerCombatState, p =>
            p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, receiver));

        if (donorPet == null || recvPet == null || lost <= 0)
            return;

        await PowerCmd.Apply<RiryokuAtkShiftDonorPower>(donorPet, lost, Owner.Creature, this);
        await PowerCmd.Apply<RiryokuAtkShiftReceiverPower>(recvPet, lost, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override bool TryGetPlayCardQueueOnActionEnqueuedDeferral(out string? reason)
    {
        reason = "riryoku";
        return true;
    }

    async Task<bool> IYgoPlayCardActionPreSpendResourceFlow.TryPreparePreSpendPlayAsync(
        PlayCardAction action,
        Player player,
        CardModel self)
    {
        List<BaseMonsterCard> BuildCandidates() => BuildFieldCandidates(player);

        var candidates = BuildCandidates();
        if (candidates.Count < 2)
            return false;

        var prefs1 = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        BaseMonsterCard? donor = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            prefs1,
            BuildCandidates);
        if (donor == null)
            return false;

        List<BaseMonsterCard> BuildReceiversForSelectedDonor() => BuildReceiverCandidates(player, donor);

        var prefs2 = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        BaseMonsterCard? receiver = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            prefs2,
            BuildReceiversForSelectedDonor);
        if (receiver == null)
            return false;

        RiryokuPlayPayload.SetPending(self, donor, receiver);
        return true;
    }

    void IYgoPlayCardActionPreSpendResourceFlow.ClearPreSpendPlayState(CardModel self) =>
        RiryokuPlayPayload.ClearForCard(self);

    private static List<BaseMonsterCard> BuildFieldCandidates(Player player) =>
        DuelMonsterFieldRegistry.OrderedFieldMonsters(player).ToList();

    private static List<BaseMonsterCard> BuildReceiverCandidates(Player player, BaseMonsterCard donor) =>
        BuildFieldCandidates(player).Where(c => !ReferenceEquals(c, donor)).ToList();
}
