using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Emergency_Provisions : BaseSpellCard
{
    public Emergency_Provisions()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && SpellTrapZonePile.CustomType.GetPile(Owner)?.Cards.Any(YgoSpellTrapZoneBridge.IsSpellOrTrapCard) == true;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature == null)
            return;

        var zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return;

        List<CardModel> candidates = zonePile.Cards
            .Where(YgoSpellTrapZoneBridge.IsSpellOrTrapCard)
            .ToList();

        if (candidates.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 0, candidates.Count)
        {
            Cancelable = true,
        };

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        List<CardModel> toDestroy = pick
            .Where(c => c.Pile?.Type == SpellTrapZonePile.CustomType && YgoSpellTrapZoneBridge.IsSpellOrTrapCard(c))
            .Distinct()
            .ToList();

        if (toDestroy.Count == 0)
            return;

        CardPile? graveyard = GraveyardPile.CustomType.GetPile(player);
        if (graveyard == null)
            return;

        bool anyField = toDestroy.Any(YgoSpellTrapZoneBridge.IsFieldSpell);

        await CardPileCmd.Add(toDestroy, graveyard, CardPilePosition.Top, this, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        if (anyField)
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);

        await CreatureCmd.Heal(player.Creature, toDestroy.Count * 1m);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
