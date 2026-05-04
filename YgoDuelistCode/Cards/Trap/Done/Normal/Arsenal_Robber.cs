using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Arsenal_Robber : BaseTrapCard
{
    public Arsenal_Robber()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Draw;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner?.PlayerCombatState != null
        && BuildEquipSpells(Owner).Count > 0;

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        List<BaseEquipSpellCard> equipSpells = BuildEquipSpells(Owner);

        if (equipSpells.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = false,
            Cancelable = true
        };

        BaseEquipSpellCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            prefs,
            () => BuildEquipSpells(Owner));
        if (chosen == null)
            return;

        CardPile? grave = YgoPlayerPiles.Graveyard(Owner);
        if (grave == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, grave, CardPilePosition.Top, chosen, false);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }

    private static List<BaseEquipSpellCard> BuildEquipSpells(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(player.PlayerCombatState.DrawPile.Cards)
        .OfType<BaseEquipSpellCard>()
        .Concat(YgoMpCombatOrder.CardsSnapshotOrderedForMp(player.PlayerCombatState.DiscardPile.Cards).OfType<BaseEquipSpellCard>())
        .ToList();
}
