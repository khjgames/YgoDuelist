using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Burning_Land : BaseContinuousSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 5m) };

    public Burning_Land()
        : base(1, CardRarity.Uncommon, TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Burning_Land),
    };

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var cs = Owner.Creature.CombatState;
        var toGy = new List<BaseFieldSpellCard>();
        foreach (Player pl in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
            toGy.AddRange(YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(pl));

        var distinct = toGy.Distinct().ToList();
        var ownersTouched = new HashSet<Player>();
        foreach (BaseFieldSpellCard fs in distinct)
        {
            Player? p = fs.Owner;
            CardPile? gy = YgoPlayerPiles.Graveyard(p);
            if (gy == null)
                continue;

            await CardPileCmd.Add(new[] { fs }, gy, CardPilePosition.Top, this, false);
            if (p != null)
                ownersTouched.Add(p);
        }

        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(ownersTouched))
        {
            YgoSpellTrapZoneBridge.SyncFromZonePile(p);
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(p);
        }

        if (ownersTouched.Count > 0)
            YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(Owner);

        if (Owner.Creature != null)
            await PowerCmd.Apply<BurningLandFieldPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(4m);
    }
}
