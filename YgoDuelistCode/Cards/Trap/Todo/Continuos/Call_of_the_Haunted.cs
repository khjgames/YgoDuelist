using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Call_of_the_Haunted : BaseContinuousTrapCard, IYgoSpellTrapEquipLink
{
    private BaseMonsterCard? _equipLinkedMonster;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public Call_of_the_Haunted()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public BaseMonsterCard? EquipLinkedMonster => _equipLinkedMonster;

    public void SetEquipLinkedMonster(BaseMonsterCard? monster) => _equipLinkedMonster = monster;

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Monster_Reborn),
        typeof(Foolish_Burial)
    };

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        GraveyardRelic.GetGraveyardCards(Owner).Any(c => c is BaseMonsterCard) &&
        DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        var graveyardMonsters = GraveyardRelic
            .GetGraveyardCards(player)
            .OfType<BaseMonsterCard>()
            .ToList();

        if (graveyardMonsters.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(
            SelectionScreenPrompt,
            1,
            1);

        var selected = await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            graveyardMonsters,
            player,
            prefs);

        var chosen = selected.FirstOrDefault() as BaseMonsterCard;
        if (chosen == null)
            return;

        // Summon the chosen monster for 0 energy cost; DuelMonsterSummon will also
        // move the card into the MonsterPile and out of Graveyard via CardPileCmd.Add.
        // Monster Reborn summons can use Attack/Defend the turn they are summoned.
        bool summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, choiceContext);
        if (summoned)
            _pendingLinkAfterZone = chosen;
    }

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (_pendingLinkAfterZone != null)
        {
            YgoSpellTrapEquipLinkRegistry.Attach(this, _pendingLinkAfterZone);
            _pendingLinkAfterZone = null;
        }

        return Task.CompletedTask;
    }
}
