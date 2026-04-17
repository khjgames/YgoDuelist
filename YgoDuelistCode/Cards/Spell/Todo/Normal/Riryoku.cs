using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Riryoku : BaseSpellCard
{
    public Riryoku()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

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
}
