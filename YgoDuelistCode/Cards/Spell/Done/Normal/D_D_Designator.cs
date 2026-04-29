using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class D_D_Designator : BaseSpellCard
{
    public D_D_Designator()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Banish;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<DdDesignatorBonusDrawPower>();
        }
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && YgoPlayerPiles.HasOtherHandCard(Owner, this);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        CardModel? toBanish = await ChooseOtherHandCard(choiceContext);
        if (toBanish == null)
            return;

        await YgoBanishedService.BanishCard(Owner, toBanish);
        await PowerCmd.Apply<DdDesignatorBonusDrawPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private async Task<CardModel?> ChooseOtherHandCard(PlayerChoiceContext choiceContext)
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
}
