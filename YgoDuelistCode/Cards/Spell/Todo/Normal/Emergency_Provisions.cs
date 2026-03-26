using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Emergency_Provisions : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

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

        if (!EmergencyProvisionsPlayPayload.TryTakePending(this, out var selectedCards) || selectedCards == null)
            return;

        List<CardModel> toDestroy = selectedCards
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

        await CreatureCmd.Heal(player.Creature, toDestroy.Count * DynamicVars["Mgc"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(1m);
    }
}
