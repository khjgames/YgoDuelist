using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While <see cref="Fairy_Box"/> is face-up.</summary>
public sealed class FairyBoxFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "FAIRY_BOX_UPKEEP"))
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        Fairy_Box? src = zone?.Cards.OfType<Fairy_Box>().FirstOrDefault(c => !c.FaceDown);
        if (src == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        CombatState? cs = player.Creature?.CombatState;
        if (cs == null)
            return;

        CardModel takeDamage = cs.CreateCard<Fairy_Box_Upkeep_Take_Damage>(player);
        CardModel destroyTrap = cs.CreateCard<Fairy_Box_Upkeep_Destroy>(player);
        var upkeepOptions = new List<CardModel> { takeDamage, destroyTrap };

        CardModel? pick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            upkeepOptions,
            player,
            canSkip: false);

        if (pick == null)
            return;

        if (pick is Fairy_Box_Upkeep_Take_Damage)
        {
            await CreatureCmd.Damage(choiceContext, Owner, 5m, ValueProp.Unpowered, Owner, src);
            await YgoFairyBoxHeadsTailsWeak.RunAfterUpkeepPaidAsync(choiceContext, cs, player, Owner, src);
            return;
        }

        if (pick is Fairy_Box_Upkeep_Destroy)
            await DestroyTrapAndRemovePowerAsync(player);
    }

    private async Task DestroyTrapAndRemovePowerAsync(Player pl)
    {
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        Fairy_Box? box = zone?.Cards.OfType<Fairy_Box>().FirstOrDefault();
        CardPile? gy = GraveyardPile.CustomType.GetPile(pl);
        if (box != null && gy != null)
            await CardPileCmd.Add(new[] { box }, gy, CardPilePosition.Top, box, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(pl);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(pl);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(pl);
        await PowerCmd.Remove(this);
    }
}
