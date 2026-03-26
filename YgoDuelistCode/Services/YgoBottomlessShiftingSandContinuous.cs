using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>While <see cref="Bottomless_Shifting_Sand"/> is face-up (no player power).</summary>
public static class YgoBottomlessShiftingSandContinuous
{
    public static async Task TryResolveAfterPlayerTurnEnd(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == null)
            return;

        Bottomless_Shifting_Sand? sand = SpellTrapZonePile.CustomType.GetPile(player)?.Cards.OfType<Bottomless_Shifting_Sand>().FirstOrDefault();
        if (sand == null)
            return;

        int handThreshold = (int)sand.DynamicVars["Mgc"].BaseValue;

        CardPile? hand = PileType.Hand.GetPile(player);
        int handCount = hand?.Cards.Count ?? 0;
        if (handCount < handThreshold)
        {
            await DestroyTrapAndSyncAsync(player);
            return;
        }

        var cs = player.Creature.CombatState;
        if (cs == null)
            return;

        var alive = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (alive.Count == 0)
            return;

        Creature? best = null;
        int bestIntent = -1;
        foreach (Creature e in YgoDeterministicRng.StableOrder(alive, c => c.CombatId))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, player.Creature);
            if (intent > bestIntent)
            {
                bestIntent = intent;
                best = e;
            }
        }

        if (best == null || bestIntent <= 0)
            return;

        decimal cap = sand.DynamicVars["Mgc2"].BaseValue;
        decimal dmg = Math.Min(bestIntent, cap);
        await CreatureCmd.Damage(choiceContext, best, dmg, ValueProp.Unpowered, player.Creature, sand);
    }

    private static async Task DestroyTrapAndSyncAsync(Player pl)
    {
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        Bottomless_Shifting_Sand? sand = zone?.Cards.OfType<Bottomless_Shifting_Sand>().FirstOrDefault();
        CardPile? gy = GraveyardPile.CustomType.GetPile(pl);
        if (sand != null && gy != null)
            await CardPileCmd.Add(new[] { sand }, gy, CardPilePosition.Top, sand, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(pl);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(pl);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(pl);
    }
}
