using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Twin_Swords_of_Flashing_Light_Tryce : BaseEquipSpellCard, IYgoPlayCardActionPreSpendResourceFlow
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 65m) };

    public Twin_Swords_of_Flashing_Light_Tryce()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Warrior;

    public override bool CardShowsPortionKeyword => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    public override StatEffectTotalMultiplier GetEquipStatMultiplier(BaseMonsterCard equipped)
    {
        decimal pct = DynamicVars["Mgc"].BaseValue / 100m;
        return new StatEffectTotalMultiplier(pct, 1m);
    }

    public override int GetEquipMinimumAttackPortionCount(BaseMonsterCard equipped) => 2;

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 80m;

    public override bool TryGetPlayCardQueueOnActionEnqueuedDeferral(out string? reason)
    {
        reason = "twin_swords_discard";
        return true;
    }

    async Task<bool> IYgoPlayCardActionPreSpendResourceFlow.TryPreparePreSpendPlayAsync(
        PlayCardAction action,
        Player player,
        CardModel self)
    {
        CardModel? discarded = await YgoHandCardSelection.TryChooseSingleHandCardAsync<CardModel>(
            YgoChoiceContexts.Blocking(),
            player,
            new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(SelectionScreenPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            c => !ReferenceEquals(c, self));

        if (discarded == null)
            return false;

        await CardPileCmd.Add(discarded, player.PlayerCombatState!.DiscardPile, CardPilePosition.Top, self, false);
        return true;
    }

    void IYgoPlayCardActionPreSpendResourceFlow.ClearPreSpendPlayState(CardModel self)
    {
    }
}
