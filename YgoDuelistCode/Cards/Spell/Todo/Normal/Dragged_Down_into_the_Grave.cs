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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Dragged_Down_into_the_Grave : BaseSpellCard
{
    public Dragged_Down_into_the_Grave()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && PileType.Hand.GetPile(Owner)?.Cards.Any(c => !ReferenceEquals(c, this)) == true;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        CardModel? toDestroy = await ChooseOtherHandCardToDestroy(choiceContext);
        if (toDestroy == null)
            return;

        await SendHandCardToGraveyard(choiceContext, Owner, toDestroy);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private async Task<CardModel?> ChooseOtherHandCardToDestroy(PlayerChoiceContext choiceContext)
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

    private static async Task SendHandCardToGraveyard(PlayerChoiceContext choiceContext, Player player, CardModel card)
    {
        CardPile? graveyardPile = GraveyardPile.CustomType.GetPile(player);
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
