using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Compulsory_Evacuation_Device : BaseTrapCard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private static readonly LocString ReturnSelectPrompt =
        new("cards", "YGODUELIST-COMPULSORY_EVACUATION_DEVICE.return_select");

    public Compulsory_Evacuation_Device()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[]
    {
        typeof(Compulsory_Evacuation_Device),
    };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner?.PlayerCombatState != null
        && BuildControlledFieldMonsters(Owner).Count > 0;

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        if (BuildControlledFieldMonsters(Owner).Count == 0)
            return;

        NormalMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(ReturnSelectPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildControlledFieldMonsters(Owner));
        if (target == null)
            return;

        await YgoFlipReturnOwnFieldMonsterToHand.TryReturnToHandAsync(Owner, target);
        if (Owner.Creature == null)
            return;

        await PlayerCmd.GainEnergy(1, Owner);
        await PlayerCmd.GainStars(1, Owner);
    }

    private static List<NormalMonsterCard> BuildControlledFieldMonsters(Player owner)
    {
        var list = new List<NormalMonsterCard>();
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState!))
        {
            if (!p.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<NormalMonsterCard>(p) is not NormalMonsterCard nm)
                continue;
            list.Add(nm);
        }

        return list;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
