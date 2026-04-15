using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>Destroy 1 card in your hand (send to Graveyard), then deal damage to targeted enemy.</summary>
public sealed class Tribute_to_the_Doomed : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 11m) };

    public Tribute_to_the_Doomed()
        : base(cost: 1, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Burn;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && PileType.Hand.GetPile(Owner)?.Cards.Any(c => !ReferenceEquals(c, this)) == true;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.Creature == null)
            return;

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        CardModel? toDestroy = await ChooseOtherHandCardToDestroy(choiceContext);
        if (toDestroy == null)
            return;

        await SendHandCardToGraveyard(choiceContext, Owner, toDestroy);

        await CreatureCmd.Damage(choiceContext, target, DynamicVars["Mgc"].BaseValue, ValueProp.Unpowered, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(5m);
    }

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

        var selected = await TributeSummonGridSelect.FromSimpleGridCombat(
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
