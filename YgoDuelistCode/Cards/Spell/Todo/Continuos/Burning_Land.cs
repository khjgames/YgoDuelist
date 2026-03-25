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
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Burning_Land : BaseContinuousSpellCard
{
    public Burning_Land()
        : base(1, CardRarity.Common, TargetType.Self)
    {
    }

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var cs = Owner.Creature.CombatState;
        var toGy = new List<BaseFieldSpellCard>();
        foreach (Player pl in cs.Players)
            toGy.AddRange(YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(pl));

        var distinct = toGy.Distinct().ToList();
        var ownersTouched = new HashSet<Player>();
        foreach (BaseFieldSpellCard fs in distinct)
        {
            Player? p = fs.Owner;
            CardPile? gy = p == null ? null : GraveyardPile.CustomType.GetPile(p);
            if (gy == null)
                continue;

            await CardPileCmd.Add(new[] { fs }, gy, CardPilePosition.Top, this, false);
            if (p != null)
                ownersTouched.Add(p);
        }

        foreach (Player p in ownersTouched)
        {
            YgoSpellTrapZoneBridge.SyncFromZonePile(p);
            YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(p);
        }

        if (ownersTouched.Count > 0)
            YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(Owner);

        if (Owner.Creature != null)
            await PowerCmd.Apply<BurningLandFieldPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
