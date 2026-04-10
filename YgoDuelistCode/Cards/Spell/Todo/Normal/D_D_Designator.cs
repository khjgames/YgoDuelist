using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class D_D_Designator : BaseSpellCard
{
    public D_D_Designator()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Banish;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && PileType.Hand.GetPile(Owner)?.Cards.Any(c => !ReferenceEquals(c, this)) == true;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        CardModel? toBanish = await ChooseOtherHandCard(choiceContext);
        if (toBanish == null)
            return;

        await YgoShadowRealmService.BanishCard(Owner, toBanish);
        await PowerCmd.Apply<DdDesignatorBonusDrawPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private async Task<CardModel?> ChooseOtherHandCard(PlayerChoiceContext choiceContext)
    {
        var hand = PileType.Hand.GetPile(Owner!);
        if (hand == null)
            return null;

        List<CardModel> candidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(Owner!, null, this);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };

        var selected = await TributeSummonGridSelect.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner!,
            prefs,
            rebuildCanonicalForRemoteApply: () => TributeSummonGridSelect.BuildStabilizedHandCandidates(Owner!, null, this),
            PlayerChoiceOptions.CancelPlayCardActions);

        return selected.FirstOrDefault();
    }
}
