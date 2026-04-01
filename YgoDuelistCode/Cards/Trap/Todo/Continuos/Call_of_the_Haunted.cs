using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Call_of_the_Haunted : BaseContinuousTrapCard, IYgoSpellTrapEquipLink, IYgoPrePlayCancelableGridSelection
{
    private BaseMonsterCard? _equipLinkedMonster;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public Call_of_the_Haunted()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }
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
        typeof(Call_of_the_Haunted),
    };

    public BaseMonsterCard? EquipLinkedMonster => _equipLinkedMonster;

    public void SetEquipLinkedMonster(BaseMonsterCard? monster) => _equipLinkedMonster = monster;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        GraveyardRelic.GetGraveyardCards(Owner).Any(c => c is BaseMonsterCard) &&
        DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        var graveyardMonsters = GraveyardRelic
            .GetGraveyardCards(player)
            .OfType<BaseMonsterCard>()
            .ToList();

        if (graveyardMonsters.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                graveyardMonsters,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        var chosen = selected.FirstOrDefault() as BaseMonsterCard;
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseMonsterCard chosen)
            return;

        if (!GraveyardRelic.GetGraveyardCards(player).Contains(chosen))
            return;

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
