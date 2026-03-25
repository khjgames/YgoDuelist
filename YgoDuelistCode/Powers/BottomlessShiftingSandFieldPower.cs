using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While <see cref="Bottomless_Shifting_Sand"/> is active.</summary>
public sealed class BottomlessShiftingSandFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BOTTOMLESS_SHIFTING_SAND_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BOTTOMLESS_SHIFTING_SAND_FIELD_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Owner.Side != CombatSide.Player)
            return;

        Player? pl = Owner.Player;
        if (pl == null)
            return;

        CardPile? hand = PileType.Hand.GetPile(pl);
        int handCount = hand?.Cards.Count ?? 0;
        if (handCount < 4)
        {
            await DestroyTrapAndRemovePowerAsync(pl);
            return;
        }

        var cs = Owner.CombatState;
        if (cs == null)
            return;

        var alive = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (alive.Count == 0)
            return;

        Creature? best = null;
        int bestIntent = -1;
        foreach (Creature e in YgoDeterministicRng.StableOrder(alive, c => c.CombatId))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, Owner);
            if (intent > bestIntent)
            {
                bestIntent = intent;
                best = e;
            }
        }

        if (best == null || bestIntent <= 0)
            return;

        decimal dmg = Math.Min(bestIntent, 30);
        Bottomless_Shifting_Sand? src = SpellTrapZonePile.CustomType.GetPile(pl)?.Cards.OfType<Bottomless_Shifting_Sand>().FirstOrDefault();
        await CreatureCmd.Damage(choiceContext, best, dmg, ValueProp.Unpowered, Owner, src);
    }

    private async Task DestroyTrapAndRemovePowerAsync(Player pl)
    {
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        Bottomless_Shifting_Sand? sand = zone?.Cards.OfType<Bottomless_Shifting_Sand>().FirstOrDefault();
        CardPile? gy = GraveyardPile.CustomType.GetPile(pl);
        if (sand != null && gy != null)
            await CardPileCmd.Add(new[] { sand }, gy, CardPilePosition.Top, sand, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(pl);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(pl);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(pl);
        await PowerCmd.Remove(this);
    }
}
