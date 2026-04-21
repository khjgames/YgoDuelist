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
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Count >= 2;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        if (!RiryokuPlayPayload.TryTakePending(this, out BaseMonsterCard? donor, out BaseMonsterCard? receiver)
            || donor == null || receiver == null)
            return;

        var field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner).OfType<BaseMonsterCard>().ToList();
        int donorAtk = (int)donor.CalcDuelMonsterStats(field).Atk;
        int newAtk = donorAtk / 2;
        int lost = donorAtk - newAtk;

        Creature? donorPet = Owner.PlayerCombatState.Pets.FirstOrDefault(p =>
            p.IsAlive && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), donor));
        Creature? recvPet = Owner.PlayerCombatState.Pets.FirstOrDefault(p =>
            p.IsAlive && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), receiver));

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
        var candidates = DuelMonsterFieldRegistry.GetFieldMonsters(player)
            .OfType<BaseMonsterCard>()
            .ToList();
        if (candidates.Count < 2)
            return false;

        var prefs1 = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        var pick1 = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs1);
        var donor = pick1.OfType<BaseMonsterCard>().FirstOrDefault();
        if (donor == null)
            return false;

        var secondList = candidates.Where(c => !ReferenceEquals(c, donor)).ToList();
        var prefs2 = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        var pick2 = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), secondList, player, prefs2);
        var receiver = pick2.OfType<BaseMonsterCard>().FirstOrDefault();
        if (receiver == null)
            return false;

        RiryokuPlayPayload.SetPending(self, donor, receiver);
        return true;
    }

    void IYgoPlayCardActionPreSpendResourceFlow.ClearPreSpendPlayState(CardModel self) =>
        RiryokuPlayPayload.ClearForCard(self);
}
