using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Cost_Down : BaseSpellCard
{
    public Cost_Down()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Cost_Down) };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && YgoPlayerPiles.HasOtherHandCard(Owner, this);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        CardModel? toDestroy = await ChooseOtherHandCardToDestroy(choiceContext);
        if (toDestroy == null)
            return;

        await SendHandCardToGraveyard(choiceContext, Owner, toDestroy);
        await PowerCmd.Apply<CostDownHandLevelPower>(Owner.Creature, 1m, Owner.Creature, this);
        CardModelEnergyCache.InvalidateHandMonstersEnergy(Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private async Task<CardModel?> ChooseOtherHandCardToDestroy(PlayerChoiceContext choiceContext)
    {
        return await YgoHandCardSelection.TryChooseSingleHandCardAsync<CardModel>(
            choiceContext,
            Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = false
            },
            excludeReference: this,
            choiceBegunOptions: PlayerChoiceOptions.CancelPlayCardActions);
    }

    private static async Task SendHandCardToGraveyard(PlayerChoiceContext choiceContext, Player player, CardModel card)
    {
        CardPile? graveyardPile = YgoPlayerPiles.Graveyard(player);
        if (graveyardPile == null)
            return;

        await CardPileCmd.Add(
            new[] { card },
            graveyardPile,
            CardPilePosition.Top,
            card,
            false);
    }
}
